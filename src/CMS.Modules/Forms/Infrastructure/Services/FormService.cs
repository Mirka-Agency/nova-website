using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Forms.Application.Actions;
using CMS.Modules.Forms.Application.AntiSpam;
using CMS.Modules.Forms.Application.Common;
using CMS.Modules.Forms.Application.Fields;
using CMS.Modules.Forms.Application.Forms;
using CMS.Modules.Forms.Application.Interfaces;
using CMS.Modules.Forms.Application.Public;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Entities;
using CMS.Modules.Forms.Domain.Enums;
using CMS.Modules.Forms.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Forms.Infrastructure.Services;

public sealed class FormService : IFormService
{
    private readonly FormsDbContext _db;
    private readonly IValidator<SaveFormCommand> _formValidator;
    private readonly IValidator<SaveFormFieldCommand> _fieldValidator;
    private readonly IFormAntiSpamService _antiSpam;

    public FormService(
        FormsDbContext db,
        IValidator<SaveFormCommand> formValidator,
        IValidator<SaveFormFieldCommand> fieldValidator,
        IFormAntiSpamService antiSpam)
    {
        _db = db;
        _formValidator = formValidator;
        _fieldValidator = fieldValidator;
        _antiSpam = antiSpam;
    }

    public async Task<IReadOnlyList<FormListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var page = await ListPagedAsync(
            new FormListRequest { Page = 1, PageSize = PagedRequest.MaxPageSize },
            cancellationToken);
        return page.Items;
    }

    public async Task<PagedResult<FormListItemDto>> ListPagedAsync(
        FormListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Forms.AsNoTracking().AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(f => f.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(f =>
                f.Name.Contains(term) || f.Slug.Contains(term) || f.Key.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        query = ApplySort(query, request.Sort);

        var items = await query
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(f => new FormListItemDto(
                f.Id,
                f.Name,
                f.Key,
                f.Slug,
                f.Status,
                f.Status == FormStatus.Published,
                f.Fields.Count,
                f.Submissions.Count,
                f.CreatedAtUtc,
                f.PublishedVersionId,
                f.DraftVersionId,
                f.IsSystem))
            .ToListAsync(cancellationToken);

        return new PagedResult<FormListItemDto>(items, total, request.NormalizedPage, request.NormalizedPageSize);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.Forms.AsNoTracking().CountAsync(cancellationToken);

    public async Task<FormDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var form = await _db.Forms
            .AsNoTracking()
            .Include(f => f.Fields)
            .Include(f => f.Versions)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        return form is null ? null : MapDetail(form);
    }

    public async Task<Guid> CreateAsync(SaveFormCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateFormAsync(command, cancellationToken);
        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), null, cancellationToken);
        var key = await EnsureUniqueKeyAsync(ResolveKey(command, slug), null, cancellationToken);
        var form = FormDefinition.Create(command.Name, key, slug, command.Description);
        ApplyFormUpdate(form, command, slug);

        _db.Forms.Add(form);
        await _db.SaveChangesAsync(cancellationToken);
        await FormVersionSync.SyncAsync(_db, form, cancellationToken);
        ApplySchemaOverrides(form, command);
        if (form.Status == FormStatus.Published)
            await FormVersionSync.PublishWorkingAsync(_db, form, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return form.Id;
    }

    public async Task<Guid> CreateFromTemplateAsync(FormTemplateKind template, CancellationToken cancellationToken = default)
    {
        var definition = FormTemplateCatalog.Get(template);
        var slug = await EnsureUniqueSlugAsync(definition.Slug, null, cancellationToken);
        var key = await EnsureUniqueKeyAsync(FormDefinition.NormalizeKey(slug), null, cancellationToken);
        var form = FormDefinition.Create(definition.Name, key, slug, definition.Description);

        foreach (var field in definition.Fields)
        {
            form.AddField(field.Key, field.Label, field.Type, field.Required, field.Options, field.Order);
        }

        _db.Forms.Add(form);
        await _db.SaveChangesAsync(cancellationToken);
        await FormVersionSync.SyncAsync(_db, form, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return form.Id;
    }

    public async Task UpdateAsync(Guid id, SaveFormCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateFormAsync(command, cancellationToken);
        var form = await _db.Forms
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(FormDefinition), id);

        var slug = await EnsureUniqueSlugAsync(ResolveSlug(command), id, cancellationToken);
        ApplyFormUpdate(form, command, slug);

        await FormVersionSync.SyncAsync(_db, form, cancellationToken);
        command = PreserveAntiSpamProviderIfMissing(form, command);
        ApplySchemaOverrides(form, command);
        // Republish whenever the form remains/becomes Published so Security-tab changes
        // (anti-spam provider, etc.) land on the live snapshot, not only the draft.
        if (form.Status == FormStatus.Published)
            await FormVersionSync.PublishWorkingAsync(_db, form, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(FormDefinition), id);

        if (form.IsSystem)
            throw new DomainException("فرم‌های سیستمی قابل حذف نیستند.");

        _db.Forms.Remove(form);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> DuplicateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var form = await _db.Forms
            .Include(f => f.Fields)
            .Include(f => f.Versions)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(FormDefinition), id);

        var sourceSchema = PreferSchema(form);

        var baseName = $"{form.Name} (کپی)";
        if (baseName.Length > 200)
            baseName = baseName[..200];

        var slug = await EnsureUniqueSlugAsync($"{form.Slug}-copy", null, cancellationToken);
        var key = await EnsureUniqueKeyAsync($"{form.Key}_copy", null, cancellationToken);
        var clone = form.Clone(baseName, key, slug);
        _db.Forms.Add(clone);
        await _db.SaveChangesAsync(cancellationToken);
        await FormVersionSync.SyncAsync(_db, clone, cancellationToken);

        if (sourceSchema is not null)
            ApplySchemaFromSource(clone, sourceSchema);

        await _db.SaveChangesAsync(cancellationToken);
        return clone.Id;
    }

    public async Task SetStatusAsync(Guid id, FormStatus status, CancellationToken cancellationToken = default)
    {
        var form = await _db.Forms.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(FormDefinition), id);

        var previousStatus = form.Status;
        form.SetStatus(status);
        await FormVersionSync.SyncAsync(_db, form, cancellationToken);
        if (status == FormStatus.Published && previousStatus != FormStatus.Published)
            await FormVersionSync.PublishWorkingAsync(_db, form, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreVersionAsync(Guid formId, Guid versionId, CancellationToken cancellationToken = default)
    {
        var form = await _db.Forms
            .Include(f => f.Fields)
            .Include(f => f.Versions)
            .FirstOrDefaultAsync(f => f.Id == formId, cancellationToken)
            ?? throw new NotFoundException(nameof(FormDefinition), formId);

        var source = form.Versions.FirstOrDefault(v => v.Id == versionId)
            ?? throw new NotFoundException(nameof(FormVersion), versionId);

        var schema = FormSchemaLegacyMapper.TryParse(source.SchemaJson)
            ?? throw new DomainException("اسکیمای نسخه نامعتبر است.");

        await FormVersionSync.SyncAsync(_db, form, cancellationToken);
        var draft = ResolveEditableVersion(form)
            ?? throw new DomainException("نسخه پیش‌نویس برای بازیابی یافت نشد.");

        var schemaJson = FormSchemaLegacyMapper.Serialize(schema);
        if (draft.State == FormVersionState.Published || draft.State == FormVersionState.Archived)
        {
            var next = form.Versions.Count == 0 ? 1 : form.Versions.Max(v => v.VersionNumber) + 1;
            draft = form.AddVersion(next, schemaJson, FormVersionState.Draft);
        }
        else
        {
            draft.ReplaceSchema(schemaJson);
            draft.MarkDraft();
        }

        form.SetVersionPointers(form.PublishedVersionId, draft.Id);
        ApplySchemaFromSource(form, schema);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> AddFieldAsync(Guid formId, SaveFormFieldCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateFieldAsync(command, cancellationToken);
        var form = await _db.Forms
            .Include(f => f.Fields)
            .Include(f => f.Versions)
            .FirstOrDefaultAsync(f => f.Id == formId, cancellationToken)
            ?? throw new NotFoundException(nameof(FormDefinition), formId);

        var settingsJson = BuildFieldSettingsJson(command, existingSettingsJson: null);
        var isRequired = IsLayoutType(command.FieldType) ? false : command.IsRequired;
        var field = form.AddField(
            command.Key,
            command.Label,
            command.FieldType,
            isRequired,
            command.OptionsCsv,
            command.SortOrder,
            command.Placeholder,
            command.HelpText,
            settingsJson);
        // Force Added: client Guids can otherwise be tracked as Modified → UPDATE 0 rows.
        _db.Entry(field).State = EntityState.Added;

        try
        {
            await PersistFieldSchemaAsync(form, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var details = string.Join("; ", ex.Entries.Select(e =>
                $"{e.Metadata.ClrType.Name}:{e.State}:Id={e.Property("Id").CurrentValue}"));
            throw new DbUpdateConcurrencyException(
                $"{ex.Message} Tracker=[{details}]",
                ex);
        }

        return field.Id;
    }

    public async Task UpdateFieldAsync(
        Guid formId,
        Guid fieldId,
        SaveFormFieldCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateFieldAsync(command, cancellationToken);
        var form = await _db.Forms
            .Include(f => f.Fields)
            .Include(f => f.Versions)
            .FirstOrDefaultAsync(f => f.Id == formId, cancellationToken)
            ?? throw new NotFoundException(nameof(FormDefinition), formId);

        var existing = form.Fields.FirstOrDefault(f => f.Id == fieldId)
            ?? throw new NotFoundException(nameof(FormField), fieldId);

        var settingsJson = BuildFieldSettingsJson(command, existing.SettingsJson);
        var isRequired = IsLayoutType(command.FieldType) ? false : command.IsRequired;
        form.UpdateField(
            fieldId,
            command.Key,
            command.Label,
            command.FieldType,
            isRequired,
            command.OptionsCsv,
            command.SortOrder,
            command.Placeholder,
            command.HelpText,
            settingsJson);

        await PersistFieldSchemaAsync(form, cancellationToken);
    }

    public async Task<Guid> DuplicateFieldAsync(Guid formId, Guid fieldId, CancellationToken cancellationToken = default)
    {
        var form = await _db.Forms
            .Include(f => f.Fields)
            .Include(f => f.Versions)
            .FirstOrDefaultAsync(f => f.Id == formId, cancellationToken)
            ?? throw new NotFoundException(nameof(FormDefinition), formId);

        if (form.Fields.All(f => f.Id != fieldId))
            throw new NotFoundException(nameof(FormField), fieldId);

        var clone = form.DuplicateField(fieldId);
        _db.Entry(clone).State = EntityState.Added;
        await PersistFieldSchemaAsync(form, cancellationToken);
        return clone.Id;
    }

    public async Task DeleteFieldAsync(Guid formId, Guid fieldId, CancellationToken cancellationToken = default)
    {
        var form = await _db.Forms
            .Include(f => f.Fields)
            .Include(f => f.Versions)
            .FirstOrDefaultAsync(f => f.Id == formId, cancellationToken)
            ?? throw new NotFoundException(nameof(FormDefinition), formId);

        var field = form.Fields.FirstOrDefault(f => f.Id == fieldId)
            ?? throw new NotFoundException(nameof(FormField), fieldId);

        form.RemoveField(fieldId);
        _db.Fields.Remove(field);
        await PersistFieldSchemaAsync(form, cancellationToken);
    }

    public async Task ReorderFieldsAsync(
        Guid formId,
        ReorderFieldsCommand command,
        CancellationToken cancellationToken = default)
    {
        var form = await _db.Forms
            .Include(f => f.Fields)
            .Include(f => f.Versions)
            .FirstOrDefaultAsync(f => f.Id == formId, cancellationToken)
            ?? throw new NotFoundException(nameof(FormDefinition), formId);

        form.ReorderFields(command.OrderedFieldIds);
        await PersistFieldSchemaAsync(form, cancellationToken);
    }

    /// <summary>
    /// Field edits always sync the draft; published forms also promote so embeds/popups
    /// see fields immediately (not only an empty submit button from a stale snapshot).
    /// </summary>
    private async Task PersistFieldSchemaAsync(FormDefinition form, CancellationToken cancellationToken)
    {
        await FormVersionSync.SyncAsync(_db, form, cancellationToken);
        if (form.Status == FormStatus.Published)
            await FormVersionSync.PublishWorkingAsync(_db, form, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<FormPublicContract?> GetPublicContractBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        return GetPublicContractAsync(f => f.Slug == normalized && f.Status == FormStatus.Published, cancellationToken);
    }

    public Task<FormPublicContract?> GetPublicContractByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var normalized = key.Trim().ToLowerInvariant();
        return GetPublicContractAsync(f => f.Key == normalized && f.Status == FormStatus.Published, cancellationToken);
    }

    public Task<FormPublicContract?> GetPublicContractByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetPublicContractAsync(f => f.Id == id && f.Status == FormStatus.Published, cancellationToken);

    private async Task<FormPublicContract?> GetPublicContractAsync(
        System.Linq.Expressions.Expression<Func<FormDefinition, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var form = await _db.Forms
            .AsNoTracking()
            .Include(f => f.Fields)
            .Include(f => f.Versions)
            .FirstOrDefaultAsync(predicate, cancellationToken);

        if (form is null)
            return null;

        var schema = ResolvePublishedSchema(form) ?? FormSchemaLegacyMapper.ToDocument(form);
        var antiSpam = _antiSpam.ResolveSettings(form, schema);
        return FormPublicContractMapper.From(form, schema, form.PublishedVersionId, antiSpam);
    }

    private static IQueryable<FormDefinition> ApplySort(IQueryable<FormDefinition> query, string? sort)
    {
        return (sort?.Trim().ToLowerInvariant()) switch
        {
            "name" => query.OrderBy(f => f.Name),
            "name_desc" => query.OrderByDescending(f => f.Name),
            "submissions" => query.OrderByDescending(f => f.Submissions.Count),
            "status" => query.OrderBy(f => f.Status).ThenByDescending(f => f.CreatedAtUtc),
            _ => query.OrderByDescending(f => f.CreatedAtUtc)
        };
    }

    private static FormDetailDto MapDetail(FormDefinition form)
    {
        var schema = PreferSchema(form);
        var submit = schema?.SubmitBehavior;
        var submitBehaviorType = string.IsNullOrWhiteSpace(submit?.Type)
            ? FormSubmitBehaviorTypes.Message
            : submit.Type.Trim().ToLowerInvariant();

        var successMessage = submit?.Message;
        var redirectUrl = submitBehaviorType is FormSubmitBehaviorTypes.Redirect or FormSubmitBehaviorTypes.Page
            ? submit?.Url
            : null;

        var submitButtonText = schema?.Settings?.SubmitButtonText;
        if (string.IsNullOrWhiteSpace(submitButtonText))
            submitButtonText = "ارسال";

        var notify = schema?.Actions.FirstOrDefault(a =>
            string.Equals(a.Type, FormActionTypeIds.EmailNotification, StringComparison.OrdinalIgnoreCase));
        var sendEmailNotification = notify is { Enabled: true };
        var notifyEmail = notify is null
            ? null
            : FormActionConfigReader.GetStringList(notify.Config, "to").FirstOrDefault();
        var notifyEmailSubject = notify is null
            ? null
            : FormActionConfigReader.GetString(notify.Config, "subject");
        var notifySenderName = notify is null
            ? null
            : FormActionConfigReader.GetString(notify.Config, "senderName");
        var notifyReplyToFieldKey = notify is null
            ? null
            : FormActionConfigReader.GetString(notify.Config, "replyToFieldKey");

        var autoReply = schema?.Actions.FirstOrDefault(a =>
            string.Equals(a.Type, FormActionTypeIds.AutoReply, StringComparison.OrdinalIgnoreCase));
        var autoReplyEnabled = autoReply is { Enabled: true };
        var autoReplySubject = autoReply is null
            ? null
            : FormActionConfigReader.GetString(autoReply.Config, "subject");
        var autoReplyBody = autoReply is null
            ? null
            : FormActionConfigReader.GetString(autoReply.Config, "body");
        var autoReplyEmailFieldKey = autoReply is null
            ? null
            : FormActionConfigReader.GetString(autoReply.Config, "recipientFieldKey");

        var webhook = schema?.Actions.FirstOrDefault(a =>
            string.Equals(a.Type, FormActionTypeIds.Webhook, StringComparison.OrdinalIgnoreCase));
        var webhookEnabled = webhook is { Enabled: true };
        var webhookUrl = webhook is null
            ? null
            : FormActionConfigReader.GetString(webhook.Config, "url");
        var webhookSecret = webhook is null
            ? null
            : FormActionConfigReader.GetString(webhook.Config, "secretKey")
              ?? FormActionConfigReader.GetString(webhook.Config, "secret");

        var antiSpam = ResolveAntiSpamDisplay(schema);
        var enableCaptcha = string.Equals(
            antiSpam.Provider,
            FormAntiSpamProviderIds.SimpleCaptcha,
            StringComparison.OrdinalIgnoreCase);

        return new(
            form.Id,
            form.Name,
            form.Key,
            form.Slug,
            form.Description,
            form.Status,
            form.IsPublished,
            form.PublishedVersionId,
            form.DraftVersionId,
            successMessage,
            redirectUrl,
            submitBehaviorType,
            submitButtonText,
            sendEmailNotification,
            notifyEmail,
            notifyEmailSubject,
            notifySenderName,
            notifyReplyToFieldKey,
            ResolveFieldId(form.Fields, notifyReplyToFieldKey),
            autoReplyEnabled,
            autoReplySubject,
            autoReplyBody,
            autoReplyEmailFieldKey,
            ResolveFieldId(form.Fields, autoReplyEmailFieldKey),
            webhookEnabled,
            webhookUrl,
            webhookSecret,
            enableCaptcha,
            antiSpam.Enabled,
            antiSpam.Provider,
            antiSpam.SiteKey,
            antiSpam.SecretKey,
            antiSpam.SimpleCaptchaExpected,
            form.Fields
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.CreatedAtUtc)
                .Select(MapField)
                .ToList(),
            form.Versions
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new FormVersionItemDto(
                    v.Id,
                    v.VersionNumber,
                    v.State,
                    v.CreatedAtUtc,
                    v.UpdatedAtUtc,
                    form.PublishedVersionId == v.Id,
                    form.DraftVersionId == v.Id))
                .ToList(),
            form.IsSystem);
    }

    private static void ApplyFormUpdate(FormDefinition form, SaveFormCommand command, string slug)
    {
        form.Update(command.Name, slug, command.Description, command.Status);
    }

    private static void ApplySchemaOverrides(FormDefinition form, SaveFormCommand command)
    {
        var version = ResolveEditableVersion(form);
        if (version is null)
            return;

        var schema = FormSchemaLegacyMapper.TryParse(version.SchemaJson)
                     ?? FormSchemaLegacyMapper.ToDocument(form);
        var updated = FormSchemaLegacyMapper.ApplySaveOverrides(schema, form, command);
        version.ReplaceSchema(FormSchemaLegacyMapper.Serialize(updated));
    }

    /// <summary>
    /// When the provider field is omitted from the POST (e.g. a disabled select), keep the
    /// value already stored on the working schema instead of resetting to honeypot.
    /// </summary>
    private static SaveFormCommand PreserveAntiSpamProviderIfMissing(FormDefinition form, SaveFormCommand command)
    {
        if (!string.IsNullOrWhiteSpace(command.AntiSpamProvider))
            return command;

        var existing = ResolveEditableVersion(form) is { } version
            ? FormSchemaLegacyMapper.TryParse(version.SchemaJson)?.AntiSpam?.Provider
            : null;
        if (string.IsNullOrWhiteSpace(existing))
            return command;

        return command with { AntiSpamProvider = existing };
    }

    private static FormVersion? ResolveEditableVersion(FormDefinition form)
    {
        if (form.DraftVersionId is Guid draftId)
        {
            var draft = form.Versions.FirstOrDefault(v => v.Id == draftId);
            if (draft is not null && draft.State != FormVersionState.Archived)
                return draft;
        }

        return form.Versions
            .Where(v => v.State != FormVersionState.Archived)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();
    }

    private static void ApplySchemaFromSource(FormDefinition clone, FormSchemaDocument source)
    {
        var version = ResolveEditableVersion(clone);
        if (version is null)
            return;

        var current = FormSchemaLegacyMapper.TryParse(version.SchemaJson)
                      ?? FormSchemaLegacyMapper.ToDocument(clone);

        var notify = source.Actions.FirstOrDefault(a =>
            string.Equals(a.Type, FormActionTypeIds.EmailNotification, StringComparison.OrdinalIgnoreCase));
        var autoReply = source.Actions.FirstOrDefault(a =>
            string.Equals(a.Type, FormActionTypeIds.AutoReply, StringComparison.OrdinalIgnoreCase));
        var webhook = source.Actions.FirstOrDefault(a =>
            string.Equals(a.Type, FormActionTypeIds.Webhook, StringComparison.OrdinalIgnoreCase));
        var antiSpam = source.AntiSpam ?? new FormAntiSpamSchema();

        var submitType = string.IsNullOrWhiteSpace(source.SubmitBehavior?.Type)
            ? FormSubmitBehaviorTypes.Message
            : source.SubmitBehavior.Type;

        var updated = FormSchemaLegacyMapper.ApplySaveOverrides(
            current,
            clone,
            submitType,
            source.SubmitBehavior?.Message,
            source.SubmitBehavior?.Url,
            source.Settings?.SubmitButtonText,
            notify is { Enabled: true },
            notify is null ? null : FormActionConfigReader.GetStringList(notify.Config, "to").FirstOrDefault(),
            notify is null ? null : FormActionConfigReader.GetString(notify.Config, "subject"),
            notify is null ? null : FormActionConfigReader.GetString(notify.Config, "senderName"),
            notify is null ? null : FormActionConfigReader.GetString(notify.Config, "replyToFieldKey"),
            autoReply is { Enabled: true },
            autoReply is null ? null : FormActionConfigReader.GetString(autoReply.Config, "subject"),
            autoReply is null ? null : FormActionConfigReader.GetString(autoReply.Config, "body"),
            autoReply is null ? null : FormActionConfigReader.GetString(autoReply.Config, "recipientFieldKey"),
            webhook is { Enabled: true },
            webhook is null ? null : FormActionConfigReader.GetString(webhook.Config, "url"),
            webhook is null
                ? null
                : FormActionConfigReader.GetString(webhook.Config, "secretKey")
                  ?? FormActionConfigReader.GetString(webhook.Config, "secret"),
            antiSpam.Enabled,
            antiSpam.Provider,
            null,
            null,
            FormActionConfigReader.GetString(antiSpam.Config, "expected"));

        version.ReplaceSchema(FormSchemaLegacyMapper.Serialize(updated));
    }

    private static (
        bool Enabled,
        string Provider,
        string? SiteKey,
        string? SecretKey,
        string? SimpleCaptchaExpected,
        IReadOnlyDictionary<string, object?> Config) ResolveAntiSpamDisplay(FormSchemaDocument? schema)
    {
        FormAntiSpamSchema settings;
        if (schema?.AntiSpam is { } fromSchema && !string.IsNullOrWhiteSpace(fromSchema.Provider))
        {
            settings = fromSchema;
        }
        else
        {
            settings = new FormAntiSpamSchema
            {
                Enabled = true,
                Provider = FormAntiSpamProviderIds.Honeypot,
                Config = new Dictionary<string, object?>()
            };
        }

        var provider = string.IsNullOrWhiteSpace(settings.Provider)
            ? FormAntiSpamProviderIds.Honeypot
            : settings.Provider.Trim();

        // Site/secret keys live in env (Forms:AntiSpam); never surface them from SchemaJson.
        return (
            settings.Enabled,
            provider,
            SiteKey: null,
            SecretKey: null,
            FormActionConfigReader.GetString(settings.Config, "expected"),
            settings.Config);
    }

    private static FormSchemaDocument? PreferSchema(FormDefinition form)
    {
        FormVersion? version = null;
        if (form.DraftVersionId is Guid draftId)
            version = form.Versions.FirstOrDefault(v => v.Id == draftId);
        version ??= form.PublishedVersionId is Guid publishedId
            ? form.Versions.FirstOrDefault(v => v.Id == publishedId)
            : null;
        version ??= form.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        return FormSchemaLegacyMapper.TryParse(version?.SchemaJson);
    }

    private static FormSchemaDocument? ResolvePublishedSchema(FormDefinition form)
    {
        FormVersion? version = null;
        if (form.PublishedVersionId is Guid publishedId)
            version = form.Versions.FirstOrDefault(v => v.Id == publishedId);
        version ??= form.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        return FormSchemaLegacyMapper.TryParse(version?.SchemaJson);
    }

    private static Guid? ResolveFieldId(IEnumerable<FormField> fields, string? fieldKey)
    {
        if (string.IsNullOrWhiteSpace(fieldKey))
            return null;

        return fields.FirstOrDefault(f =>
                f.Key.Equals(fieldKey.Trim(), StringComparison.OrdinalIgnoreCase))
            ?.Id;
    }

    private static FormFieldDto MapField(FormField field)
    {
        var extras = FormFieldSchemaFactory.ParseExtras(field.SettingsJson);
        return new FormFieldDto(
            field.Id,
            field.Key,
            field.Label,
            field.FieldType,
            field.IsRequired,
            field.OptionsCsv,
            field.Placeholder,
            field.HelpText,
            field.SettingsJson,
            field.SortOrder,
            extras.LayoutWidth,
            extras.DefaultValue,
            field.IsSystem);
    }

    private static string? BuildFieldSettingsJson(SaveFormFieldCommand command, string? existingSettingsJson)
    {
        var legacy = FormFieldSchemaFactory.ParseExtras(command.SettingsJson ?? existingSettingsJson);
        var extras = new FormFieldFormExtras
        {
            DefaultValue = command.DefaultValue,
            LayoutWidth = string.IsNullOrWhiteSpace(command.LayoutWidth) ? "full" : command.LayoutWidth,
            MinLength = command.MinLength,
            MaxLength = command.MaxLength,
            Min = command.Min,
            Max = command.Max,
            Pattern = command.Pattern,
            AllowedExtensions = command.AllowedExtensions,
            MaxFileSizeMb = command.MaxFileSizeMb,
            MinSelections = command.MinSelections,
            MaxSelections = command.MaxSelections,
            CaptchaExpected = legacy.CaptchaExpected,
            Visibility = command.Visibility
        };
        return FormFieldSchemaFactory.BuildSettingsJson(extras, command.SettingsJson ?? existingSettingsJson);
    }

    private static bool IsLayoutType(FormFieldType type) =>
        type is FormFieldType.Heading or FormFieldType.Paragraph or FormFieldType.Divider;

    private async Task ValidateFormAsync(SaveFormCommand command, CancellationToken cancellationToken)
    {
        var result = await _formValidator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private async Task ValidateFieldAsync(SaveFormFieldCommand command, CancellationToken cancellationToken)
    {
        var result = await _fieldValidator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private static string ResolveSlug(SaveFormCommand command)
    {
        var slug = SlugGenerator.FromTitle(command.Slug ?? string.Empty);
        if (string.IsNullOrWhiteSpace(slug))
            slug = SynonymSlug(command.Name);

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                [nameof(command.Slug)] = ["نامک الزامی است."]
            });
        }

        return slug;
    }

    private static string ResolveKey(SaveFormCommand command, string slug)
    {
        var raw = !string.IsNullOrWhiteSpace(command.Key) ? command.Key : slug;
        FormDefinition.ValidateKey(raw);
        return FormDefinition.NormalizeKey(raw);
    }

    private static string SynonymSlug(string name)
    {
        var fromTitle = SlugGenerator.FromTitle(name);
        if (!string.IsNullOrWhiteSpace(fromTitle))
            return fromTitle;

        return $"form-{Guid.NewGuid():N}"[..16];
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, Guid? excludeId, CancellationToken cancellationToken)
    {
        var candidate = slug;
        var suffix = 2;
        while (await _db.Forms.AnyAsync(
                   f => f.Slug == candidate && (!excludeId.HasValue || f.Id != excludeId.Value),
                   cancellationToken))
        {
            candidate = $"{slug}-{suffix}";
            suffix++;
        }

        return candidate;
    }

    private async Task<string> EnsureUniqueKeyAsync(string key, Guid? excludeId, CancellationToken cancellationToken)
    {
        var normalized = FormDefinition.NormalizeKey(key);
        var candidate = normalized;
        var suffix = 2;
        while (await _db.Forms.AnyAsync(
                   f => f.Key == candidate && (!excludeId.HasValue || f.Id != excludeId.Value),
                   cancellationToken))
        {
            candidate = $"{normalized}_{suffix}";
            suffix++;
        }

        return candidate;
    }
}
