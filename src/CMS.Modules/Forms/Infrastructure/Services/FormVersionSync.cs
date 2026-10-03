using System.Text.Json;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Entities;
using CMS.Modules.Forms.Domain.Enums;
using CMS.Modules.Forms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Forms.Infrastructure.Services;

/// <summary>
/// Mutates form version rows in-memory only. Callers must SaveChanges once afterwards.
/// Field/settings edits always go to the draft snapshot; publishing is explicit via
/// <see cref="PublishWorking"/>.
/// </summary>
internal static class FormVersionSync
{
    public static async Task SyncAsync(FormsDbContext db, FormDefinition form, CancellationToken cancellationToken)
    {
        await EnsureCollectionsLoadedAsync(db, form, cancellationToken);

        var fieldsDocument = FormSchemaLegacyMapper.ToDocument(form);
        var working = ResolveWorkingVersion(form);
        var existing = working is null ? null : FormSchemaLegacyMapper.TryParse(working.SchemaJson);
        var merged = existing is null
            ? fieldsDocument
            : new FormSchemaDocument
            {
                SchemaVersion = existing.SchemaVersion <= 0
                    ? FormSchemaDocument.CurrentSchemaVersion
                    : existing.SchemaVersion,
                Fields = fieldsDocument.Fields,
                SubmitBehavior = existing.SubmitBehavior,
                Actions = SanitizeActions(existing.Actions),
                AntiSpam = SanitizeAntiSpam(existing.AntiSpam),
                Settings = existing.Settings
            };

        var schemaJson = FormSchemaLegacyMapper.Serialize(merged);
        // Always write into the editable draft. Never promote published snapshots here —
        // that path was creating phantom UPDATEs / concurrency failures on every field save.
        UpdateDraft(db, form, working, schemaJson);
    }

    /// <summary>
    /// Promote the current working schema to a new published snapshot (and keep a draft copy).
    /// Call only when the form is (becoming) Published.
    /// </summary>
    public static async Task PublishWorkingAsync(FormsDbContext db, FormDefinition form, CancellationToken cancellationToken)
    {
        await EnsureCollectionsLoadedAsync(db, form, cancellationToken);
        var working = ResolveWorkingVersion(form);
        var schemaJson = working?.SchemaJson ?? FormSchemaLegacyMapper.ToSchemaJson(form);
        PublishSchema(db, form, working, schemaJson);
    }

    private static async Task EnsureCollectionsLoadedAsync(
        FormsDbContext db,
        FormDefinition form,
        CancellationToken cancellationToken)
    {
        var fields = db.Entry(form).Collection(f => f.Fields);
        var hasPendingFields = db.ChangeTracker.Entries<FormField>()
            .Any(e => e.Entity.FormId == form.Id
                      && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
        if (!fields.IsLoaded && !hasPendingFields)
            await fields.LoadAsync(cancellationToken);

        var versions = db.Entry(form).Collection(f => f.Versions);
        if (!versions.IsLoaded)
            await versions.LoadAsync(cancellationToken);
    }

    private static void UpdateDraft(
        FormsDbContext db,
        FormDefinition form,
        FormVersion? working,
        string schemaJson)
    {
        if (working is null || working.State == FormVersionState.Archived)
        {
            var version = form.AddVersion(NextVersionNumber(form), schemaJson, FormVersionState.Draft);
            TrackNewVersion(db, version);
            form.SetVersionPointers(form.PublishedVersionId, version.Id);
            return;
        }

        if (working.State == FormVersionState.Published)
        {
            // Keep published snapshot frozen; create a separate draft for edits.
            var draft = form.AddVersion(NextVersionNumber(form), schemaJson, FormVersionState.Draft);
            TrackNewVersion(db, draft);
            form.SetVersionPointers(working.Id, draft.Id);
            return;
        }

        working.ReplaceSchema(schemaJson);
        working.MarkDraft();
        form.SetVersionPointers(form.PublishedVersionId, working.Id);
    }

    private static void PublishSchema(
        FormsDbContext db,
        FormDefinition form,
        FormVersion? working,
        string schemaJson)
    {
        var published = form.PublishedVersionId is Guid publishedId
            ? form.Versions.FirstOrDefault(v => v.Id == publishedId)
            : null;

        // First publish: promote working draft (or create) to published.
        if (published is null)
        {
            if (working is null || working.State == FormVersionState.Archived)
            {
                var version = form.AddVersion(NextVersionNumber(form), schemaJson, FormVersionState.Published);
                TrackNewVersion(db, version);
                form.SetVersionPointers(version.Id, version.Id);
                return;
            }

            working.ReplaceSchema(schemaJson);
            working.MarkPublished();
            form.SetVersionPointers(working.Id, working.Id);
            return;
        }

        // Schema unchanged: keep current published pointer.
        if (string.Equals(published.SchemaJson, schemaJson, StringComparison.Ordinal))
        {
            published.MarkPublished();
            var draftId = form.DraftVersionId is Guid d && d != published.Id
                ? d
                : published.Id;
            form.SetVersionPointers(published.Id, draftId);
            return;
        }

        // Snapshot history: archive previous published, create new published version.
        FormVersion newPublished;
        if (working is not null
            && working.Id != published.Id
            && working.State != FormVersionState.Archived)
        {
            published.Archive();
            working.ReplaceSchema(schemaJson);
            working.MarkPublished();
            newPublished = working;
        }
        else
        {
            newPublished = form.AddVersion(NextVersionNumber(form), schemaJson, FormVersionState.Published);
            TrackNewVersion(db, newPublished);
            published.Archive();
        }

        // Keep an editable draft copy aligned with published.
        var draft = form.AddVersion(NextVersionNumber(form), schemaJson, FormVersionState.Draft);
        TrackNewVersion(db, draft);

        foreach (var orphan in form.Versions.Where(v =>
                     v.State == FormVersionState.Draft
                     && v.Id != draft.Id
                     && v.Id != newPublished.Id))
        {
            orphan.Archive();
        }

        form.SetVersionPointers(newPublished.Id, draft.Id);
    }

    private static void TrackNewVersion(FormsDbContext db, FormVersion version)
    {
        var entry = db.Entry(version);
        // Client-generated Guids + ValueGeneratedOnAdd conventions can leave new
        // navigation entities as Unchanged/Modified → UPDATE 0 rows.
        if (entry.State != EntityState.Added)
            entry.State = EntityState.Added;
    }

    private static FormVersion? ResolveWorkingVersion(FormDefinition form)
    {
        if (form.DraftVersionId is Guid draftId)
        {
            var draft = form.Versions.FirstOrDefault(v => v.Id == draftId);
            if (draft is not null)
                return draft;
        }

        if (form.PublishedVersionId is Guid publishedId)
        {
            var published = form.Versions.FirstOrDefault(v => v.Id == publishedId);
            if (published is not null)
                return published;
        }

        return form.Versions
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();
    }

    private static int NextVersionNumber(FormDefinition form) =>
        form.Versions.Count == 0 ? 1 : form.Versions.Max(v => v.VersionNumber) + 1;

    private static IReadOnlyList<FormActionSchema> SanitizeActions(IReadOnlyList<FormActionSchema> actions) =>
        actions.Select(a => new FormActionSchema
        {
            Id = a.Id,
            Type = a.Type,
            Enabled = a.Enabled,
            Config = SanitizeConfig(a.Config)
        }).ToList();

    private static FormAntiSpamSchema SanitizeAntiSpam(FormAntiSpamSchema antiSpam) =>
        new()
        {
            Enabled = antiSpam.Enabled,
            Provider = antiSpam.Provider,
            Config = SanitizeConfig(antiSpam.Config)
        };

    private static IReadOnlyDictionary<string, object?> SanitizeConfig(IReadOnlyDictionary<string, object?>? config)
    {
        if (config is null || config.Count == 0)
            return new Dictionary<string, object?>();

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in config)
            result[key] = SanitizeValue(value);
        return result;
    }

    private static object? SanitizeValue(object? value)
    {
        if (value is null)
            return null;

        if (value is JsonElement el)
            return SimplifyJsonElement(el);

        if (value is string or bool or byte or short or int or long or float or double or decimal)
            return value;

        try
        {
            var json = JsonSerializer.SerializeToElement(value);
            return SimplifyJsonElement(json);
        }
        catch
        {
            return value.ToString();
        }
    }

    private static object? SimplifyJsonElement(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => el.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when el.TryGetInt64(out var l) => l,
            JsonValueKind.Number when el.TryGetDecimal(out var d) => d,
            JsonValueKind.Array => el.EnumerateArray().Select(SimplifyJsonElement).ToList(),
            JsonValueKind.Object => el.EnumerateObject()
                .ToDictionary(p => p.Name, p => SimplifyJsonElement(p.Value), StringComparer.OrdinalIgnoreCase),
            _ => el.GetRawText()
        };

    public static Guid RequirePublishedVersionId(FormDefinition form)
    {
        if (form.PublishedVersionId is Guid id && id != Guid.Empty)
            return id;

        var published = form.Versions.FirstOrDefault(v => v.State == FormVersionState.Published);
        if (published is not null)
            return published.Id;

        throw new InvalidOperationException(
            $"Form '{form.Key}' is published but has no PublishedVersionId. Run FormEnginePhase1Seeder.");
    }
}
