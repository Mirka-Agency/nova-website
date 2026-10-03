using CMS.Modules.Forms.Application.Forms;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Entities;
using CMS.Modules.Forms.Domain.Enums;
using CMS.Modules.Forms.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMS.Modules.Forms.Infrastructure.Persistence;

/// <summary>
/// Phase 1: one-time-style backfill for Form version pointers, SchemaJson, and Submission.FormVersionId
/// when missing after migration. Must not overwrite existing Published/Draft pointers on later deploys.
/// </summary>
public static class FormEnginePhase1Seeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FormsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Forms.FormEnginePhase1Seeder");

        var forms = await db.Forms
            .Include(f => f.Fields)
            .Include(f => f.Versions)
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var form in forms)
        {
            if (await EnsureVersionAsync(db, form, cancellationToken))
                changed = true;
            if (await RepairEmptyPublishedFieldsAsync(db, form, logger, cancellationToken))
                changed = true;
        }

        var orphanSubmissions = await db.Submissions
            .Where(s => s.FormVersionId == Guid.Empty)
            .ToListAsync(cancellationToken);

        foreach (var submission in orphanSubmissions)
        {
            var versionId = await db.Forms
                .Where(f => f.Id == submission.FormId)
                .Select(f => f.PublishedVersionId ?? f.DraftVersionId)
                .FirstOrDefaultAsync(cancellationToken);

            if (versionId is null || versionId == Guid.Empty)
                continue;

            // FormVersionId is private-set; use entry property for one-time backfill.
            db.Entry(submission).Property(nameof(FormSubmission.FormVersionId)).CurrentValue = versionId.Value;
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Forms Phase1 seeder applied version/schema backfill.");
        }
    }

    public static async Task<bool> EnsureVersionAsync(
        FormsDbContext db,
        FormDefinition form,
        CancellationToken cancellationToken = default)
    {
        var schemaJson = FormSchemaLegacyMapper.ToSchemaJson(form);
        var existing = form.Versions.OrderBy(v => v.VersionNumber).FirstOrDefault()
                       ?? await db.FormVersions
                           .Where(v => v.FormId == form.Id)
                           .OrderBy(v => v.VersionNumber)
                           .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            var state = form.Status == FormStatus.Published
                ? FormVersionState.Published
                : FormVersionState.Draft;
            var version = form.AddVersion(1, schemaJson, state);
            db.FormVersions.Add(version);
            await db.SaveChangesAsync(cancellationToken);

            if (form.Status == FormStatus.Published)
                form.SetVersionPointers(version.Id, version.Id);
            else
                form.SetVersionPointers(null, version.Id);

            return true;
        }

        var touched = false;
        if (string.IsNullOrWhiteSpace(existing.SchemaJson) || existing.SchemaJson is "{}" or "null")
        {
            existing.ReplaceSchema(schemaJson);
            touched = true;
        }

        // Only backfill missing pointers. Never force Published/Draft back onto the oldest
        // version — that wiped admin anti-spam (and other schema) choices on every deploy.
        var publishedId = form.PublishedVersionId;
        var draftId = form.DraftVersionId;
        if (publishedId is null && form.Status == FormStatus.Published)
            publishedId = existing.Id;
        if (draftId is null)
            draftId = existing.Id;

        if (form.PublishedVersionId != publishedId || form.DraftVersionId != draftId)
        {
            form.SetVersionPointers(publishedId, draftId);
            touched = true;
        }

        if (form.Status == FormStatus.Published
            && form.PublishedVersionId is Guid pointerId
            && form.Versions.FirstOrDefault(v => v.Id == pointerId) is { } published
            && published.State != FormVersionState.Published)
        {
            published.MarkPublished();
            touched = true;
        }

        return touched;
    }

    /// <summary>
    /// Heal published forms that only show an empty submit button because the live
    /// schema lost its fields while the draft/legacy rows (or contact template) still have them.
    /// </summary>
    internal static async Task<bool> RepairEmptyPublishedFieldsAsync(
        FormsDbContext db,
        FormDefinition form,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (form.Status != FormStatus.Published)
            return false;

        var published = form.PublishedVersionId is Guid publishedId
            ? form.Versions.FirstOrDefault(v => v.Id == publishedId)
            : null;
        var publishedSchema = FormSchemaLegacyMapper.TryParse(published?.SchemaJson);
        if (publishedSchema?.Fields is { Count: > 0 })
            return false;

        if (form.Fields.Count == 0
            && string.Equals(form.Key, "contact", StringComparison.OrdinalIgnoreCase))
        {
            var template = FormTemplateCatalog.Get(FormTemplateKind.Contact);
            foreach (var field in template.Fields)
            {
                var created = form.AddField(
                    field.Key, field.Label, field.Type, field.Required, field.Options, field.Order);
                db.Entry(created).State = EntityState.Added;
            }

            logger.LogWarning(
                "Repaired empty published contact form {FormId} by seeding template fields.",
                form.Id);
        }

        if (form.Fields.Count == 0)
        {
            var draftSchema = form.DraftVersionId is Guid draftId
                ? FormSchemaLegacyMapper.TryParse(form.Versions.FirstOrDefault(v => v.Id == draftId)?.SchemaJson)
                : null;
            if (draftSchema?.Fields is not { Count: > 0 })
                return false;

            // Draft has fields but legacy table is empty — rebuild rows from draft schema keys
            // is out of scope; just publish the draft snapshot.
            await FormVersionSync.PublishWorkingAsync(db, form, cancellationToken);
            logger.LogWarning(
                "Republished form {FormKey} ({FormId}) from draft because published schema had no fields.",
                form.Key,
                form.Id);
            return true;
        }

        await FormVersionSync.SyncAsync(db, form, cancellationToken);
        await FormVersionSync.PublishWorkingAsync(db, form, cancellationToken);
        logger.LogWarning(
            "Republished form {FormKey} ({FormId}) so live schema includes {FieldCount} fields.",
            form.Key,
            form.Id,
            form.Fields.Count);
        return true;
    }
}
