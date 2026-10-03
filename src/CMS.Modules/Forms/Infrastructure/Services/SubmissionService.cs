using System.Text;
using System.Text.Json;
using CMS.Application.Background;
using CMS.Application.Common.Paging;
using CMS.Application.Email;
using CMS.Application.Sms;
using CMS.Application.Settings;
using CMS.Application.Storage;
using CMS.Domain.Exceptions;
using CMS.Modules.Forms.Application.Actions;
using CMS.Modules.Forms.Application.AntiSpam;
using CMS.Modules.Forms.Application.Common;
using CMS.Modules.Forms.Application.Fields;
using CMS.Modules.Forms.Application.Interfaces;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Application.Submissions;
using CMS.Modules.Forms.Domain.Entities;
using CMS.Modules.Forms.Domain.Enums;
using CMS.Modules.Forms.Domain.Fields;
using CMS.Modules.Forms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Forms.Infrastructure.Services;

public sealed class SubmissionService : ISubmissionService
{
    private static readonly string[] PreviewKeys = ["name", "email", "phone", "message", "full_name", "fullname"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly FormsDbContext _db;
    private readonly IObjectStorage _objectStorage;
    private readonly IFormFieldTypeRegistry _fieldRegistry;
    private readonly IFormActionExecutor _actionExecutor;
    private readonly IFormAntiSpamService _antiSpam;
    private readonly ISiteSettingsService? _siteSettings;
    private readonly IBackgroundTaskQueue _backgroundQueue;
    private readonly ILogger<SubmissionService> _logger;
    private readonly IAdminNotifier? _adminNotifier;

    public SubmissionService(
        FormsDbContext db,
        IObjectStorage objectStorage,
        IFormFieldTypeRegistry fieldRegistry,
        IFormActionExecutor actionExecutor,
        IFormAntiSpamService antiSpam,
        IBackgroundTaskQueue backgroundQueue,
        ILogger<SubmissionService> logger,
        ISiteSettingsService? siteSettings = null,
        IAdminNotifier? adminNotifier = null)
    {
        _db = db;
        _objectStorage = objectStorage;
        _fieldRegistry = fieldRegistry;
        _actionExecutor = actionExecutor;
        _antiSpam = antiSpam;
        _backgroundQueue = backgroundQueue;
        _logger = logger;
        _siteSettings = siteSettings;
        _adminNotifier = adminNotifier;
    }

    public async Task<IReadOnlyList<SubmissionListItemDto>> ListAsync(
        Guid? formId = null,
        CancellationToken cancellationToken = default)
    {
        var page = await ListPagedAsync(
            new SubmissionListRequest { FormId = formId, Page = 1, PageSize = PagedRequest.MaxPageSize },
            cancellationToken);
        return page.Items;
    }

    public async Task<PagedResult<SubmissionListItemDto>> ListPagedAsync(
        SubmissionListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = BuildFilterQuery(request);
        var total = await query.CountAsync(cancellationToken);

        var submissions = await query
            .OrderByDescending(s => s.SubmittedAtUtc)
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(s => new
            {
                s.Id,
                s.FormId,
                FormName = s.Form.Name,
                s.SubmittedAtUtc,
                s.Status,
                s.IpAddress,
                s.DataJson
            })
            .ToListAsync(cancellationToken);

        var items = submissions.Select(s => new SubmissionListItemDto(
            s.Id,
            s.FormId,
            s.FormName,
            s.SubmittedAtUtc,
            s.Status,
            BuildPreviewFromDataJson(s.DataJson),
            s.IpAddress)).ToList();

        return new PagedResult<SubmissionListItemDto>(items, total, request.NormalizedPage, request.NormalizedPageSize);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.Submissions.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> CountUnreadAsync(CancellationToken cancellationToken = default) =>
        _db.Submissions.AsNoTracking().CountAsync(s => s.Status == SubmissionStatus.New, cancellationToken);

    public async Task<SubmissionDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var submission = await _db.Submissions
            .AsNoTracking()
            .Include(s => s.Form)
                .ThenInclude(f => f.Fields)
            .Include(s => s.Form)
                .ThenInclude(f => f.Versions)
            .Include(s => s.Files)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (submission is null)
            return null;

        return MapDetail(submission);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var submission = await _db.Submissions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(FormSubmission), id);

        _db.Submissions.Remove(submission);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task BulkDeleteAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return;

        var submissions = await _db.Submissions.Where(s => ids.Contains(s.Id)).ToListAsync(cancellationToken);
        _db.Submissions.RemoveRange(submissions);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var submission = await GetTrackedAsync(id, cancellationToken);
        submission.MarkRead();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkUnreadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var submission = await GetTrackedAsync(id, cancellationToken);
        submission.MarkUnread();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var submission = await GetTrackedAsync(id, cancellationToken);
        submission.Archive();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnarchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var submission = await GetTrackedAsync(id, cancellationToken);
        submission.Unarchive();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task BulkSetStatusAsync(
        IReadOnlyList<Guid> ids,
        SubmissionStatus status,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return;

        var submissions = await _db.Submissions.Where(s => ids.Contains(s.Id)).ToListAsync(cancellationToken);
        foreach (var submission in submissions)
        {
            switch (status)
            {
                case SubmissionStatus.Read:
                    submission.MarkRead();
                    break;
                case SubmissionStatus.New:
                    submission.MarkUnread();
                    break;
                case SubmissionStatus.Archived:
                    submission.Archive();
                    break;
                case SubmissionStatus.Processed:
                    submission.MarkProcessed();
                    break;
                case SubmissionStatus.Spam:
                    submission.MarkSpam();
                    break;
                default:
                    throw new DomainException("وضعیت پاسخ نامعتبر است.");
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Stream> ExportCsvAsync(
        ExportSubmissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Submissions
            .AsNoTracking()
            .Include(s => s.Form)
                .ThenInclude(f => f.Fields)
            .Include(s => s.Form)
                .ThenInclude(f => f.Versions)
            .Include(s => s.Files)
            .AsQueryable();

        if (request.Ids is { Count: > 0 })
        {
            query = query.Where(s => request.Ids.Contains(s.Id));
        }
        else
        {
            if (request.FormId.HasValue)
                query = query.Where(s => s.FormId == request.FormId.Value);
            if (request.Status.HasValue)
                query = query.Where(s => s.Status == request.Status.Value);
            if (request.FromUtc.HasValue)
                query = query.Where(s => s.SubmittedAtUtc >= request.FromUtc.Value);
            if (request.ToUtc.HasValue)
                query = query.Where(s => s.SubmittedAtUtc <= request.ToUtc.Value);
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim();
                query = query.Where(s =>
                    s.Form.Name.Contains(term)
                    || s.DataJson.Contains(term));
            }
        }

        var stream = new MemoryStream();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true);

        await writer.WriteLineAsync(
            "SubmissionId,FormName,SubmittedAtUtc,Status,IpAddress,Page,Referrer,UtmSource,UtmMedium,UtmCampaign,FieldKey,FieldLabel,Value");

        await foreach (var submission in query
                           .OrderByDescending(s => s.SubmittedAtUtc)
                           .AsAsyncEnumerable()
                           .WithCancellation(cancellationToken))
        {
            var context = TryParseContext(submission.ContextJson);
            var rows = ResolveExportRows(submission);
            if (rows.Count == 0)
            {
                await writer.WriteLineAsync(string.Join(',',
                    Csv(submission.Id.ToString()),
                    Csv(submission.Form.Name),
                    Csv(submission.SubmittedAtUtc.ToString("O")),
                    Csv(submission.Status.ToString()),
                    Csv(submission.IpAddress),
                    Csv(context?.Page),
                    Csv(context?.Referrer),
                    Csv(context?.UtmSource),
                    Csv(context?.UtmMedium),
                    Csv(context?.UtmCampaign),
                    "\"\"",
                    "\"\"",
                    "\"\""));
                continue;
            }

            foreach (var row in rows)
            {
                await writer.WriteLineAsync(string.Join(',',
                    Csv(submission.Id.ToString()),
                    Csv(submission.Form.Name),
                    Csv(submission.SubmittedAtUtc.ToString("O")),
                    Csv(submission.Status.ToString()),
                    Csv(submission.IpAddress),
                    Csv(context?.Page),
                    Csv(context?.Referrer),
                    Csv(context?.UtmSource),
                    Csv(context?.UtmMedium),
                    Csv(context?.UtmCampaign),
                    Csv(row.FieldKey),
                    Csv(row.FieldLabel),
                    Csv(row.Value)));
            }
        }

        await writer.FlushAsync(cancellationToken);
        stream.Position = 0;
        return stream;
    }

    public async Task<SubmitResultDto> SubmitAsync(SubmitFormCommand command, CancellationToken cancellationToken = default)
    {
        var slug = command.Slug.Trim().ToLowerInvariant();
        var form = await _db.Forms
            .Include(f => f.Fields)
            .Include(f => f.Versions)
            .FirstOrDefaultAsync(f => f.Slug == slug && f.Status == FormStatus.Published, cancellationToken)
            ?? throw new NotFoundException(nameof(FormDefinition), slug);

        var filesByKey = command.Files
            .GroupBy(f => f.FieldKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var normalizedValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var validFiles = new Dictionary<string, SubmittedFile>(StringComparer.OrdinalIgnoreCase);

        var schema = ResolveSchema(form);
        var schemaFields = schema.Fields is { Count: > 0 }
            ? schema.Fields.ToList()
            : form.Fields.Select(f => FormFieldSchemaFactory.FromEntity(f, _fieldRegistry)).ToList();

        var valuesByFieldIdOrKey = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in form.Fields)
        {
            command.Values.TryGetValue(f.Key, out var raw);
            valuesByFieldIdOrKey[f.Key] = raw;
            valuesByFieldIdOrKey[f.Id.ToString("D")] = raw;
        }

        foreach (var sf in schemaFields)
        {
            if (valuesByFieldIdOrKey.ContainsKey(sf.Key))
                continue;
            if (!command.Values.TryGetValue(sf.Key, out var raw))
                continue;
            valuesByFieldIdOrKey[sf.Key] = raw;
            if (!string.IsNullOrWhiteSpace(sf.Id))
                valuesByFieldIdOrKey[sf.Id] = raw;
        }

        var antiSpamSettings = _antiSpam.ResolveSettings(form, schema);
        var antiSpamResult = await _antiSpam.ValidateAsync(
            new FormAntiSpamValidationRequest
            {
                Settings = antiSpamSettings,
                Form = form,
                HoneypotValue = command.Honeypot,
                CaptchaAnswer = command.CaptchaAnswer,
                ProviderToken = command.AntiSpamToken,
                Values = command.Values,
                RemoteIp = command.IpAddress
            },
            cancellationToken);

        if (!antiSpamResult.IsValid)
        {
            if (antiSpamResult.Errors.ContainsKey("honeypot"))
                throw new DomainException("ارسال رد شد.");

            foreach (var (key, messages) in antiSpamResult.Errors)
                errors[key] = messages;
        }

        foreach (var field in form.Fields.OrderBy(f => f.SortOrder))
        {
            var handler = _fieldRegistry.GetForLegacy(field.FieldType);
            var fieldSchema = ResolveFieldSchema(field, schemaFields);

            if (field.FieldType == FormFieldType.Captcha)
                continue;

            if (!FormFieldVisibilityEvaluator.IsVisible(fieldSchema, valuesByFieldIdOrKey, schemaFields))
                continue;

            FormFieldFileInput? fileInput = null;
            filesByKey.TryGetValue(field.Key, out var submittedFile);
            if (field.FieldType == FormFieldType.FileUpload && submittedFile is not null && submittedFile.Length > 0)
            {
                fileInput = new FormFieldFileInput
                {
                    FileName = submittedFile.FileName,
                    ContentType = submittedFile.ContentType,
                    Length = submittedFile.Length,
                    Content = submittedFile.Content
                };
            }

            command.Values.TryGetValue(field.Key, out var raw);
            var result = handler.Validate(new FormFieldValidationRequest
            {
                Field = fieldSchema,
                RawValue = raw,
                File = fileInput
            });

            if (!result.IsValid)
                errors[field.Key] = [result.Error!];

            if (result.SkipPersist || !handler.IsInput)
                continue;

            if (field.FieldType == FormFieldType.FileUpload)
            {
                if (submittedFile is not null && submittedFile.Length > 0 && result.IsValid)
                    validFiles[field.Key] = submittedFile;
                continue;
            }

            normalizedValues[field.Key] = result.NormalizedValue;
        }

        if (errors.Count > 0)
            throw new DomainValidationException(errors);

        var formVersionId = FormVersionSync.RequirePublishedVersionId(form);
        var contextJson = SerializeContext(command.Context);

        // Create submission first so file rows can bind to it; data JSON finalized after uploads.
        var submission = form.Submit(formVersionId, command.IpAddress, command.UserAgent, null, contextJson);

        var dataFields = new List<SubmissionDataField>();

        foreach (var field in form.Fields.OrderBy(f => f.SortOrder))
        {
            var handler = _fieldRegistry.GetForLegacy(field.FieldType);
            if (!handler.IsInput)
                continue;

            var typeId = _fieldRegistry.ToTypeId(field.FieldType);

            if (field.FieldType == FormFieldType.FileUpload)
            {
                if (!validFiles.TryGetValue(field.Key, out var file))
                    continue;

                if (file.Content.CanSeek)
                    file.Content.Position = 0;

                var objectKey = ObjectStorageKeys.Create(
                    ObjectStorageKeys.Modules.Forms,
                    form.Slug,
                    file.FileName);
                var upload = await _objectStorage.UploadAsync(
                    file.Content,
                    objectKey,
                    file.ContentType,
                    cancellationToken);

                var stored = submission.AddFile(
                    field.Id,
                    field.Key,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    upload.ObjectKey,
                    upload.PublicUrl);

                var publicUrl = Truncate(upload.PublicUrl, 4000);
                dataFields.Add(new SubmissionDataField
                {
                    FieldId = field.Id.ToString("D"),
                    Key = field.Key,
                    Label = field.Label,
                    Type = typeId,
                    Value = publicUrl,
                    FileId = stored.Id.ToString("D")
                });
                continue;
            }

            if (!normalizedValues.TryGetValue(field.Key, out var value))
                continue;

            var truncated = Truncate(value, 4000);
            string? displayValue = null;
            if (field.FieldType is FormFieldType.Select or FormFieldType.Radio or FormFieldType.CheckboxGroup)
            {
                var pairs = FormFieldOptionParser.Parse(field.OptionsCsv);
                displayValue = FormFieldOptionParser.ToDisplayLabels(truncated, pairs);
                if (string.Equals(displayValue, truncated, StringComparison.Ordinal))
                    displayValue = null;
            }

            dataFields.Add(new SubmissionDataField
            {
                FieldId = field.Id.ToString("D"),
                Key = field.Key,
                Label = field.Label,
                Type = typeId,
                Value = truncated,
                DisplayValue = displayValue
            });
        }

        // DataJson is the sole store for submission field data.
        var dataJson = JsonSerializer.Serialize(new SubmissionDataDocument { Fields = dataFields }, JsonOptions);
        submission.SetDataJson(dataJson);

        await _db.SaveChangesAsync(cancellationToken);

        var formId = form.Id;
        var submissionId = submission.Id;
        var formName = form.Name;
        var pageContext = command.Context?.Page;
        await _backgroundQueue.QueueAsync(async (sp, ct) =>
        {
            var db = sp.GetRequiredService<FormsDbContext>();
            var actionExecutor = sp.GetRequiredService<IFormActionExecutor>();
            var fieldRegistry = sp.GetRequiredService<IFormFieldTypeRegistry>();
            var logger = sp.GetRequiredService<ILogger<SubmissionService>>();
            var siteSettings = sp.GetService<ISiteSettingsService>();
            var adminNotifier = sp.GetService<IAdminNotifier>();

            var loadedForm = await db.Forms
                .AsNoTracking()
                .Include(f => f.Fields)
                .Include(f => f.Versions)
                .FirstOrDefaultAsync(f => f.Id == formId, ct);
            var loadedSubmission = await db.Submissions
                .AsNoTracking()
                .Include(s => s.Files)
                .FirstOrDefaultAsync(s => s.Id == submissionId, ct);
            if (loadedForm is null || loadedSubmission is null)
                return;

            string? siteName = null;
            try
            {
                if (siteSettings is not null)
                    siteName = (await siteSettings.GetAsync(ct)).SiteName;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not load site settings for form action templates");
            }

            var schema = FormSchemaLegacyMapper.TryParse(
                             loadedForm.Versions
                                 .OrderByDescending(v => v.VersionNumber)
                                 .FirstOrDefault(v => loadedForm.PublishedVersionId == null || v.Id == loadedForm.PublishedVersionId)
                                 ?.SchemaJson)
                         ?? FormSchemaLegacyMapper.ToDocument(loadedForm);

            var context = FormActionExecutionContextFactory.Create(
                loadedForm,
                loadedSubmission,
                schema,
                siteName,
                pageContext);

            await actionExecutor.ExecuteAllAsync(context, ct);

            if (adminNotifier is not null)
            {
                try
                {
                    await adminNotifier.NotifyAdminsAsync(
                        $"ثبت فرم «{formName}»",
                        $"فرم جدید «{formName}» در سایت ثبت شد.",
                        ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Admin notify for form submission failed");
                }
            }
        }, cancellationToken);

        var (successMessage, redirectUrl) = FormSubmitBehaviorResolver.Resolve(schema, null, null);

        return new SubmitResultDto(submission.Id, successMessage, redirectUrl);
    }

    private static FormSchemaDocument ResolveSchema(FormDefinition form)
    {
        FormVersion? version = null;
        if (form.PublishedVersionId is Guid publishedId)
            version = form.Versions.FirstOrDefault(v => v.Id == publishedId);
        version ??= form.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

        return FormSchemaLegacyMapper.TryParse(version?.SchemaJson)
               ?? FormSchemaLegacyMapper.ToDocument(form);
    }

    private FormSchemaField ResolveFieldSchema(FormField field, IReadOnlyList<FormSchemaField> schemaFields)
    {
        var id = field.Id.ToString("D");
        return schemaFields.FirstOrDefault(s =>
                   string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(s.Key, field.Key, StringComparison.OrdinalIgnoreCase))
               ?? FormFieldSchemaFactory.FromEntity(field, _fieldRegistry);
    }

    private async Task ExecuteActionsAsync(
        FormDefinition form,
        FormSubmission submission,
        FormSchemaDocument schema,
        SubmitFormCommand command,
        CancellationToken cancellationToken)
    {
        string? siteName = null;
        try
        {
            if (_siteSettings is not null)
                siteName = (await _siteSettings.GetAsync(cancellationToken)).SiteName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load site settings for form action templates");
        }

        var context = FormActionExecutionContextFactory.Create(
            form,
            submission,
            schema,
            siteName,
            command.Context?.Page);

        await _actionExecutor.ExecuteAllAsync(context, cancellationToken);
    }

    private IQueryable<FormSubmission> BuildFilterQuery(SubmissionListRequest request)
    {
        var query = _db.Submissions.AsNoTracking().AsQueryable();

        if (request.FormId.HasValue)
            query = query.Where(s => s.FormId == request.FormId.Value);
        if (request.Status.HasValue)
            query = query.Where(s => s.Status == request.Status.Value);
        else
            // Default inbox hides archived/spam; use status filter or Archive nav for those.
            query = query.Where(s =>
                s.Status != SubmissionStatus.Archived && s.Status != SubmissionStatus.Spam);
        if (request.FromUtc.HasValue)
            query = query.Where(s => s.SubmittedAtUtc >= request.FromUtc.Value);
        if (request.ToUtc.HasValue)
            query = query.Where(s => s.SubmittedAtUtc <= request.ToUtc.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(s =>
                s.Form.Name.Contains(term)
                || s.DataJson.Contains(term));
        }

        return query;
    }

    private async Task<FormSubmission> GetTrackedAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Submissions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
        ?? throw new NotFoundException(nameof(FormSubmission), id);

    private static SubmissionDetailDto MapDetail(FormSubmission submission)
    {
        var schemaByKey = BuildSchemaFieldMap(submission);
        var values = ResolveValues(submission, schemaByKey);
        var files = submission.Files
            .OrderBy(f => f.CreatedAtUtc)
            .Select(f => new SubmissionFileDto(
                f.Id,
                f.FieldId,
                f.FieldKey,
                f.OriginalFileName,
                f.ContentType,
                f.SizeBytes,
                f.PublicUrl))
            .ToList();

        return new(
            submission.Id,
            submission.FormId,
            submission.FormVersionId,
            submission.Form.Name,
            submission.SubmittedAtUtc,
            submission.Status,
            submission.IpAddress,
            submission.UserAgent,
            TryParseContext(submission.ContextJson),
            values,
            files);
    }

    private static IReadOnlyList<SubmissionValueDto> ResolveValues(
        FormSubmission submission,
        IReadOnlyDictionary<string, FormSchemaField>? schemaByKey)
    {
        var fromData = TryParseDataFields(submission.DataJson);
        if (fromData is null)
            return [];

        return fromData
            .Select(f =>
            {
                var display = SubmissionValueDisplay.Resolve(f, schemaByKey);
                return new SubmissionValueDto(
                    Guid.TryParse(f.FieldId, out var id) ? id : null,
                    f.Key,
                    f.Label,
                    JalaliDateHelper.FormatStoredValue(display, f.Type),
                    f.Type);
            })
            .ToList();
    }

    private static IReadOnlyList<(string FieldKey, string? FieldLabel, string? Value)> ResolveExportRows(
        FormSubmission submission)
    {
        var fromData = TryParseDataFields(submission.DataJson);
        if (fromData is null)
            return [];

        var schemaByKey = BuildSchemaFieldMap(submission);
        var filesById = submission.Files.ToDictionary(f => f.Id.ToString("D"), StringComparer.OrdinalIgnoreCase);
        return fromData.Select(f =>
        {
            var value = JalaliDateHelper.FormatStoredValue(
                SubmissionValueDisplay.Resolve(f, schemaByKey),
                f.Type);
            if (!string.IsNullOrWhiteSpace(f.FileId)
                && filesById.TryGetValue(f.FileId, out var file))
            {
                value = !string.IsNullOrWhiteSpace(file.PublicUrl)
                    ? file.PublicUrl
                    : file.OriginalFileName;
            }

            return (f.Key, f.Label, value);
        }).ToList();
    }

    private static IReadOnlyDictionary<string, FormSchemaField>? BuildSchemaFieldMap(FormSubmission submission)
    {
        try
        {
            FormSchemaDocument? schema = null;
            if (submission.Form is not null)
            {
                FormVersion? version = null;
                if (submission.FormVersionId != Guid.Empty)
                    version = submission.Form.Versions?.FirstOrDefault(v => v.Id == submission.FormVersionId);
                version ??= submission.Form.PublishedVersionId is Guid publishedId
                    ? submission.Form.Versions?.FirstOrDefault(v => v.Id == publishedId)
                    : null;
                version ??= submission.Form.Versions?.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

                schema = FormSchemaLegacyMapper.TryParse(version?.SchemaJson)
                         ?? (submission.Form.Fields is { Count: > 0 }
                             ? FormSchemaLegacyMapper.ToDocument(submission.Form)
                             : null);
            }

            if (schema?.Fields is not { Count: > 0 })
                return null;

            return schema.Fields
                .Where(f => !string.IsNullOrWhiteSpace(f.Key))
                .GroupBy(f => f.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return null;
        }
    }

    private static string? BuildPreviewFromDataJson(string? dataJson)
    {
        var fromData = TryParseDataFields(dataJson);
        if (fromData is null)
            return null;

        return BuildPreview(fromData.Select(f =>
            (f.Key, SubmissionValueDisplay.Resolve(f.Value, f.DisplayValue, f.Type, null))));
    }

    private static string? BuildPreview(IEnumerable<(string FieldKey, string? Value)> values)
    {
        var list = values.ToList();
        foreach (var key in PreviewKeys)
        {
            var match = list.FirstOrDefault(v =>
                v.FieldKey.Equals(key, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(v.Value));
            if (!string.IsNullOrWhiteSpace(match.Value))
                return Truncate(match.Value, 120);
        }

        return Truncate(list.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.Value)).Value, 120);
    }

    private static IReadOnlyList<SubmissionDataField>? TryParseDataFields(string? dataJson)
    {
        if (string.IsNullOrWhiteSpace(dataJson))
            return null;

        try
        {
            var doc = JsonSerializer.Deserialize<SubmissionDataDocument>(dataJson, JsonOptions);
            if (doc?.Fields is { Count: > 0 })
                return doc.Fields;
        }
        catch (JsonException)
        {
            // Invalid DataJson — treat as empty.
        }

        return null;
    }

    private static SubmissionContextDocument? TryParseContext(string? contextJson)
    {
        if (string.IsNullOrWhiteSpace(contextJson) || contextJson.Trim() == "{}")
            return null;

        try
        {
            return JsonSerializer.Deserialize<SubmissionContextDocument>(contextJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string SerializeContext(SubmissionContextDocument? context)
    {
        if (context is null)
            return "{}";

        return JsonSerializer.Serialize(context, JsonOptions);
    }

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r'))
            return $"\"{text.Replace("\"", "\"\"")}\"";
        return text;
    }

    private static string? Truncate(string? value, int max)
    {
        if (value is null)
            return null;
        return value.Length <= max ? value : value[..max];
    }
}
