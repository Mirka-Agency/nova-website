using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Forms.Application.Common;
using CMS.Modules.Forms.Application.Fields;
using CMS.Modules.Forms.Application.Forms;
using CMS.Modules.Forms.Application.Interfaces;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Application.Submissions;
using CMS.Modules.Forms.Domain.Enums;
using CMS.Modules.Forms.Web.Areas.Admin.ViewModels;
using CMS.Modules.Forms.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Forms.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewForms")]
public class FormsController : Controller
{
    private readonly IFormService _forms;
    private readonly ISubmissionService _submissions;
    private readonly IFormFieldTypeRegistry _fieldRegistry;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<FormsAdmin> _localizer;
    private readonly ILogger<FormsController> _logger;

    public FormsController(
        IFormService forms,
        ISubmissionService submissions,
        IFormFieldTypeRegistry fieldRegistry,
        IFeatureManager features,
        IStringLocalizer<FormsAdmin> localizer,
        ILogger<FormsController> logger)
    {
        _forms = forms;
        _submissions = submissions;
        _fieldRegistry = fieldRegistry;
        _features = features;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IActionResult> Index(
        int page = 1,
        string? q = null,
        FormStatus? status = null,
        string? sort = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        ViewData["Title"] = _localizer["Forms"].Value;
        ViewData["Templates"] = FormTemplateCatalog.List();

        var result = await _forms.ListPagedAsync(
            new FormListRequest { Page = page, Search = q, Status = status, Sort = sort },
            cancellationToken);

        var model = new FormIndexViewModel
        {
            Search = q,
            Status = status,
            Sort = sort,
            Page = result.Page,
            TotalPages = result.TotalPages,
            TotalCount = result.TotalCount,
            Items = result.Items.Select(f => new FormListItemViewModel
            {
                Id = f.Id,
                Name = f.Name,
                Key = f.Key,
                Slug = f.Slug,
                Status = f.Status,
                StatusLabel = StatusLabel(f.Status),
                FieldCount = f.FieldCount,
                SubmissionCount = f.SubmissionCount,
                CreatedAtUtc = f.CreatedAtUtc,
                IsSystem = f.IsSystem
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> Create()
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        ViewData["Title"] = _localizer["CreateForm"].Value;
        return View(PrepareForm(new FormFormViewModel()));
    }

    [HttpPost]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> Create(FormFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        model = PrepareForm(model);
        ViewData["Title"] = _localizer["CreateForm"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var id = await _forms.CreateAsync(ToCommand(model), cancellationToken);
            _logger.LogInformation("Admin action: created form {FormId}", id);
            TempData["Success"] = _localizer["FormCreated"].Value;
            return RedirectToAction(nameof(Edit), new { area = "Admin", id, tab = "fields" });
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create form");
            ModelState.AddModelError(string.Empty, _localizer["FormCreateFailed"].Value);
            return View(model);
        }
    }

    [HttpPost]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> CreateFromTemplate(FormTemplateKind template, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        if (!Enum.IsDefined(template))
            return NotFound();

        try
        {
            var id = await _forms.CreateFromTemplateAsync(template, cancellationToken);
            _logger.LogInformation("Admin action: created form {FormId} from template {Template}", id, template);
            TempData["Success"] = _localizer["FormCreatedFromTemplate"].Value;
            return RedirectToAction(nameof(Edit), new { area = "Admin", id, tab = "fields" });
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create form from template {Template}", template);
            TempData["Error"] = _localizer["FormCreateFailed"].Value;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, string? tab = null, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        var form = await _forms.GetAsync(id, cancellationToken);
        if (form is null)
            return NotFound();

        ViewData["Title"] = _localizer["EditForm"].Value;
        var model = await MapFormAsync(form, tab, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> Edit(Guid id, FormFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        model.Id = id;
        model.ActiveTab = NormalizeTab(model.ActiveTab);
        var existing = await _forms.GetAsync(id, cancellationToken);
        if (existing is null)
            return NotFound();

        model.Fields = MapFields(existing);
        model.SubmissionCount = await CountSubmissionsAsync(id, cancellationToken);
        // Disabled/hidden anti-spam controls may omit AntiSpamProvider from the POST.
        // Preserve the stored provider instead of letting PrepareForm/ToCommand default to honeypot.
        if (string.IsNullOrWhiteSpace(model.AntiSpamProvider))
            model.AntiSpamProvider = existing.AntiSpamProvider;
        model = PrepareForm(model);
        ViewData["Title"] = _localizer["EditForm"].Value;

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _forms.UpdateAsync(id, ToCommand(model), cancellationToken);
            _logger.LogInformation("Admin action: updated form {FormId}", id);
            TempData["Success"] = _localizer["FormUpdated"].Value;
            return RedirectToAction(nameof(Edit), new { id, tab = model.ActiveTab });
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        try
        {
            await _forms.DeleteAsync(id, cancellationToken);
            _logger.LogInformation("Admin action: deleted form {FormId}", id);
            TempData["Success"] = _localizer["FormDeleted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        try
        {
            var cloneId = await _forms.DuplicateAsync(id, cancellationToken);
            TempData["Success"] = _localizer["FormDuplicated"].Value;
            return RedirectToAction(nameof(Edit), new { id = cloneId, tab = "fields" });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> SetStatus(Guid id, FormStatus status, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        try
        {
            await _forms.SetStatusAsync(id, status, cancellationToken);
            TempData["Success"] = _localizer["FormStatusUpdated"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> ReorderFields(Guid formId, [FromForm] Guid[] orderedIds, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        try
        {
            await _forms.ReorderFieldsAsync(formId, new ReorderFieldsCommand(orderedIds), cancellationToken);
            return Ok(new { ok = true });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> AddField(Guid formId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        var form = await _forms.GetAsync(formId, cancellationToken);
        if (form is null)
            return NotFound();

        ViewData["Title"] = _localizer["AddField"].Value;
        ViewData["FormName"] = form.Name;
        var nextOrder = form.Fields.Count == 0 ? 1 : form.Fields.Max(f => f.SortOrder) + 1;
        return View("FieldForm", await BuildFieldFormAsync(formId, new FormFieldFormViewModel
        {
            FormId = formId,
            SortOrder = nextOrder
        }, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> AddField(FormFieldFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        model = await BuildFieldFormAsync(model.FormId, model, cancellationToken);
        ViewData["Title"] = _localizer["AddField"].Value;

        if (!ModelState.IsValid)
            return View("FieldForm", model);

        try
        {
            await _forms.AddFieldAsync(model.FormId, await ToFieldCommandAsync(model, cancellationToken), cancellationToken);
            TempData["Success"] = _localizer["FieldCreated"].Value;
            return RedirectToAction(nameof(Edit), new { id = model.FormId, tab = "fields" });
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
            return View("FieldForm", model);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("FieldForm", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add field for form {FormId}", model.FormId);
            var detail = ex.GetBaseException().Message;
            ModelState.AddModelError(string.Empty, $"{_localizer["FieldSaveFailed"].Value} ({detail})");
            return View("FieldForm", model);
        }
    }

    [HttpGet]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> EditField(Guid formId, Guid fieldId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        var form = await _forms.GetAsync(formId, cancellationToken);
        var field = form?.Fields.FirstOrDefault(f => f.Id == fieldId);
        if (form is null || field is null)
            return NotFound();

        ViewData["Title"] = _localizer["EditField"].Value;
        ViewData["FormName"] = form.Name;
        var extras = FormFieldSchemaFactory.ParseExtras(field.SettingsJson);
        var visibility = extras.Visibility;
        var visibilityMode = visibility?.Conditions is { Count: > 0 }
            ? (string.Equals(visibility.Mode, "any", StringComparison.OrdinalIgnoreCase) ? "any" : "all")
            : "always";
        var conditionRows = visibility?.Conditions is { Count: > 0 }
            ? visibility.Conditions.Select(c => new FormFieldVisibilityConditionRow
            {
                FieldId = c.FieldId,
                Operator = string.IsNullOrWhiteSpace(c.Operator) ? "equals" : c.Operator,
                Value = c.Value
            }).ToList()
            : [];
        return View("FieldForm", await BuildFieldFormAsync(formId, new FormFieldFormViewModel
        {
            FormId = formId,
            FieldId = field.Id,
            IsSystem = field.IsSystem,
            FormIsSystem = form.IsSystem,
            Key = field.Key,
            Label = field.Label,
            FieldType = field.FieldType,
            IsRequired = field.IsRequired,
            OptionsCsv = field.OptionsCsv,
            Placeholder = field.Placeholder,
            HelpText = field.HelpText,
            SettingsJson = field.SettingsJson,
            DefaultValue = field.DefaultValue ?? extras.DefaultValue,
            LayoutWidth = string.IsNullOrWhiteSpace(field.LayoutWidth) ? (extras.LayoutWidth ?? "full") : field.LayoutWidth!,
            MinLength = extras.MinLength,
            MaxLength = extras.MaxLength,
            Min = extras.Min,
            Max = extras.Max,
            Pattern = extras.Pattern,
            AllowedExtensions = extras.AllowedExtensions,
            MaxFileSizeMb = extras.MaxFileSizeMb,
            MinSelections = extras.MinSelections,
            MaxSelections = extras.MaxSelections,
            VisibilityMode = visibilityMode,
            VisibilityConditions = conditionRows,
            SortOrder = field.SortOrder
        }, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> EditField(FormFieldFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        if (model.FieldId is not Guid fieldId)
            return NotFound();

        model = await BuildFieldFormAsync(model.FormId, model, cancellationToken);
        ViewData["Title"] = _localizer["EditField"].Value;

        if (!ModelState.IsValid)
            return View("FieldForm", model);

        try
        {
            await _forms.UpdateFieldAsync(model.FormId, fieldId, await ToFieldCommandAsync(model, cancellationToken), cancellationToken);
            TempData["Success"] = _localizer["FieldUpdated"].Value;
            return RedirectToAction(nameof(Edit), new { id = model.FormId, tab = "fields" });
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
            return View("FieldForm", model);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("FieldForm", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update field {FieldId} for form {FormId}", fieldId, model.FormId);
            var detail = ex.GetBaseException().Message;
            ModelState.AddModelError(string.Empty, $"{_localizer["FieldSaveFailed"].Value} ({detail})");
            return View("FieldForm", model);
        }
    }

    [HttpPost]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> DuplicateField(Guid formId, Guid fieldId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        try
        {
            await _forms.DuplicateFieldAsync(formId, fieldId, cancellationToken);
            TempData["Success"] = _localizer["FieldDuplicated"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { id = formId, tab = "fields" });
    }

    [HttpPost]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> RestoreVersion(Guid formId, Guid versionId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        try
        {
            await _forms.RestoreVersionAsync(formId, versionId, cancellationToken);
            TempData["Success"] = _localizer["VersionRestored"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { id = formId, tab = "advanced" });
    }

    [HttpPost]
    [Authorize(Policy = "ManageForms")]
    public async Task<IActionResult> DeleteField(Guid formId, Guid fieldId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        try
        {
            await _forms.DeleteFieldAsync(formId, fieldId, cancellationToken);
            TempData["Success"] = _localizer["FieldDeleted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { id = formId, tab = "fields" });
    }

    private async Task<FormFieldFormViewModel> BuildFieldFormAsync(
        Guid formId,
        FormFieldFormViewModel? model,
        CancellationToken cancellationToken)
    {
        model ??= new FormFieldFormViewModel { FormId = formId };
        model.FormId = formId;

        if (string.IsNullOrWhiteSpace(model.VisibilityMode))
            model.VisibilityMode = "always";
        model.VisibilityConditions ??= [];
        if (model.VisibilityConditions.Count == 0
            && (model.VisibilityMode is "all" or "any"))
        {
            model.VisibilityConditions.Add(new FormFieldVisibilityConditionRow());
        }

        foreach (var row in model.VisibilityConditions)
        {
            if (string.IsNullOrWhiteSpace(row.Operator))
                row.Operator = "equals";
        }

        var selectable = _fieldRegistry.AdminSelectable
            .Where(h => h.LegacyEnum.HasValue)
            .Select(h => h.LegacyEnum!.Value)
            .ToList();

        if (model.FieldType == FormFieldType.Captcha && !selectable.Contains(FormFieldType.Captcha))
            selectable.Add(FormFieldType.Captcha);

        model.FieldTypeOptions = selectable
            .OrderBy(t => t.ToString())
            .Select(t => new SelectListItem(
                _localizer[$"FieldType_{t}"].Value,
                t.ToString(),
                t == model.FieldType))
            .ToList();

        model.LayoutWidthOptions =
        [
            new SelectListItem(_localizer["LayoutWidthFull"].Value, "full", model.LayoutWidth == "full"),
            new SelectListItem(_localizer["LayoutWidthHalf"].Value, "half", model.LayoutWidth == "half")
        ];

        model.VisibilityModeOptions =
        [
            new SelectListItem(_localizer["VisibilityModeAlways"].Value, "always", model.VisibilityMode == "always"),
            new SelectListItem(_localizer["VisibilityModeAll"].Value, "all", model.VisibilityMode == "all"),
            new SelectListItem(_localizer["VisibilityModeAny"].Value, "any", model.VisibilityMode == "any")
        ];

        model.VisibilityOperatorOptions =
        [
            new SelectListItem(_localizer["VisibilityOperatorEquals"].Value, "equals"),
            new SelectListItem(_localizer["VisibilityOperatorNotEquals"].Value, "not_equals"),
            new SelectListItem(_localizer["VisibilityOperatorContains"].Value, "contains"),
            new SelectListItem(_localizer["VisibilityOperatorIsEmpty"].Value, "is_empty"),
            new SelectListItem(_localizer["VisibilityOperatorIsNotEmpty"].Value, "is_not_empty")
        ];

        var form = await _forms.GetAsync(formId, cancellationToken);
        model.FormIsSystem = form?.IsSystem == true;
        if (model.FieldId is Guid fieldId)
        {
            var existing = form?.Fields.FirstOrDefault(f => f.Id == fieldId);
            if (existing is not null)
                model.IsSystem = existing.IsSystem;
        }

        model.VisibilityFieldOptions = (form?.Fields ?? [])
            .Where(f => f.Id != model.FieldId)
            .Where(f => f.FieldType is not FormFieldType.Heading and not FormFieldType.Paragraph and not FormFieldType.Divider)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Label)
            .Select(f => new SelectListItem($"{f.Label} ({f.Key})", f.Id.ToString("D")))
            .ToList();

        return model;
    }

    private FormFormViewModel PrepareForm(FormFormViewModel model)
    {
        model.StatusOptions = Enum.GetValues<FormStatus>()
            .Select(s => new SelectListItem(StatusLabel(s), s.ToString(), s == model.Status))
            .ToList();

        if (string.IsNullOrWhiteSpace(model.SubmitBehaviorType))
            model.SubmitBehaviorType = "message";

        model.AntiSpamProvider = string.IsNullOrWhiteSpace(model.AntiSpamProvider)
            ? "honeypot"
            : model.AntiSpamProvider.Trim().ToLowerInvariant();

        model.SubmitBehaviorOptions =
        [
            new SelectListItem(_localizer["SubmitBehavior_Message"].Value, "message",
                string.Equals(model.SubmitBehaviorType, "message", StringComparison.OrdinalIgnoreCase)),
            new SelectListItem(_localizer["SubmitBehavior_Redirect"].Value, "redirect",
                string.Equals(model.SubmitBehaviorType, "redirect", StringComparison.OrdinalIgnoreCase)),
            new SelectListItem(_localizer["SubmitBehavior_Page"].Value, "page",
                string.Equals(model.SubmitBehaviorType, "page", StringComparison.OrdinalIgnoreCase))
        ];

        model.AntiSpamProviderOptions =
        [
            new SelectListItem(_localizer["AntiSpamProvider_Honeypot"].Value, "honeypot",
                string.Equals(model.AntiSpamProvider, "honeypot", StringComparison.OrdinalIgnoreCase)),
            new SelectListItem(_localizer["AntiSpamProvider_SimpleCaptcha"].Value, "simple_captcha",
                string.Equals(model.AntiSpamProvider, "simple_captcha", StringComparison.OrdinalIgnoreCase)),
            new SelectListItem(_localizer["AntiSpamProvider_Turnstile"].Value, "turnstile",
                string.Equals(model.AntiSpamProvider, "turnstile", StringComparison.OrdinalIgnoreCase)),
            new SelectListItem(_localizer["AntiSpamProvider_Recaptcha"].Value, "recaptcha",
                string.Equals(model.AntiSpamProvider, "recaptcha", StringComparison.OrdinalIgnoreCase)),
            new SelectListItem(_localizer["AntiSpamProvider_Hcaptcha"].Value, "hcaptcha",
                string.Equals(model.AntiSpamProvider, "hcaptcha", StringComparison.OrdinalIgnoreCase))
        ];

        model.EnableCaptcha = string.Equals(model.AntiSpamProvider, "simple_captcha", StringComparison.OrdinalIgnoreCase)
                              || model.EnableCaptcha;
        return model;
    }

    private string StatusLabel(FormStatus status) => status switch
    {
        FormStatus.Published => _localizer["Published"].Value,
        FormStatus.Disabled => _localizer["Disabled"].Value,
        _ => _localizer["Draft"].Value
    };

    private static SaveFormCommand ToCommand(FormFormViewModel model)
    {
        var provider = string.IsNullOrWhiteSpace(model.AntiSpamProvider)
            ? "honeypot"
            : model.AntiSpamProvider.Trim().ToLowerInvariant();
        var enableCaptcha = model.EnableCaptcha
                            || string.Equals(provider, "simple_captcha", StringComparison.OrdinalIgnoreCase);

        return new(
            model.Name,
            model.Id is null ? model.Key : null,
            model.Slug,
            model.Description,
            model.Publish ? FormStatus.Published : FormStatus.Draft,
            model.SuccessMessage,
            model.RedirectUrl,
            model.SubmitButtonText,
            model.SendEmailNotification,
            model.NotifyEmail,
            model.NotifyEmailSubject,
            model.NotifySenderName,
            model.NotifyReplyToFieldKey,
            model.NotifyReplyToFieldId,
            model.AutoReplyEnabled,
            model.AutoReplySubject,
            model.AutoReplyBody,
            model.AutoReplyEmailFieldKey,
            model.AutoReplyEmailFieldId,
            enableCaptcha,
            model.SubmitBehaviorType,
            model.WebhookEnabled,
            model.WebhookUrl,
            model.WebhookSecret,
            model.SendWhatsAppNotification,
            model.WhatsAppGroupId,
            model.WhatsAppGroupName,
            model.WhatsAppTemplate,
            model.AntiSpamEnabled,
            provider,
            null,
            null,
            model.SimpleCaptchaExpected);
    }

    private async Task<SaveFormFieldCommand> ToFieldCommandAsync(
        FormFieldFormViewModel model,
        CancellationToken cancellationToken)
    {
        var key = ResolveFieldKey(model.Key, model.Label);
        var sortOrder = model.SortOrder;
        if (sortOrder <= 0 && model.FieldId is null)
        {
            var form = await _forms.GetAsync(model.FormId, cancellationToken);
            sortOrder = form is null || form.Fields.Count == 0
                ? 1
                : form.Fields.Max(f => f.SortOrder) + 1;
        }

        return new SaveFormFieldCommand(
            key,
            model.Label,
            model.FieldType,
            model.IsRequired,
            model.OptionsCsv,
            model.Placeholder,
            model.HelpText,
            model.SettingsJson,
            sortOrder < 0 ? 0 : sortOrder,
            model.DefaultValue,
            model.LayoutWidth,
            model.MinLength,
            model.MaxLength,
            model.Min,
            model.Max,
            model.Pattern,
            model.AllowedExtensions,
            model.MaxFileSizeMb,
            model.MinSelections,
            model.MaxSelections,
            BuildVisibility(model));
    }

    private static FormFieldVisibilitySchema? BuildVisibility(FormFieldFormViewModel model)
    {
        var mode = model.VisibilityMode?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(mode) || mode == "always")
            return null;

        var conditions = (model.VisibilityConditions ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.FieldId) && !string.IsNullOrWhiteSpace(c.Operator))
            .Select(c => new FormVisibilityConditionSchema
            {
                FieldId = c.FieldId!.Trim(),
                Operator = c.Operator.Trim().ToLowerInvariant(),
                Value = c.Value
            })
            .ToList();

        if (conditions.Count == 0)
            return null;

        return new FormFieldVisibilitySchema
        {
            Mode = mode == "any" ? "any" : "all",
            Conditions = conditions
        };
    }

    private static string ResolveFieldKey(string? key, string label)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            var trimmed = key.Trim().ToLowerInvariant();
            if (IsAsciiFieldKey(trimmed))
                return trimmed.Length > 100 ? trimmed[..100] : trimmed;
        }

        var fromLabel = SlugGenerator.FromTitle(label).Replace('-', '_');
        if (!string.IsNullOrWhiteSpace(fromLabel) && IsAsciiFieldKey(fromLabel))
            return fromLabel.Length > 100 ? fromLabel[..100] : fromLabel;

        return $"field_{Guid.NewGuid():N}"[..14];
    }

    private static bool IsAsciiFieldKey(string key) =>
        key.Length > 0 && key.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '-');


    private async Task<FormFormViewModel> MapFormAsync(
        FormDetailDto form,
        string? tab,
        CancellationToken cancellationToken)
    {
        var model = PrepareForm(new FormFormViewModel
        {
            Id = form.Id,
            ActiveTab = NormalizeTab(tab),
            SubmissionCount = await CountSubmissionsAsync(form.Id, cancellationToken),
            IsSystem = form.IsSystem,
            Name = form.Name,
            Key = form.Key,
            Slug = form.Slug,
            Description = form.Description,
            Status = form.Status,
            Publish = form.Status == FormStatus.Published,
            SuccessMessage = form.SuccessMessage,
            RedirectUrl = form.RedirectUrl,
            SubmitBehaviorType = form.SubmitBehaviorType,
            SubmitButtonText = form.SubmitButtonText,
            SendEmailNotification = form.SendEmailNotification,
            NotifyEmail = form.NotifyEmail,
            NotifyEmailSubject = form.NotifyEmailSubject,
            NotifySenderName = form.NotifySenderName,
            NotifyReplyToFieldKey = form.NotifyReplyToFieldKey,
            NotifyReplyToFieldId = form.NotifyReplyToFieldId,
            AutoReplyEnabled = form.AutoReplyEnabled,
            AutoReplySubject = form.AutoReplySubject,
            AutoReplyBody = form.AutoReplyBody,
            AutoReplyEmailFieldKey = form.AutoReplyEmailFieldKey,
            AutoReplyEmailFieldId = form.AutoReplyEmailFieldId,
            WebhookEnabled = form.WebhookEnabled,
            WebhookUrl = form.WebhookUrl,
            WebhookSecret = form.WebhookSecret,
            SendWhatsAppNotification = form.SendWhatsAppNotification,
            WhatsAppGroupId = form.WhatsAppGroupId,
            WhatsAppGroupName = form.WhatsAppGroupName,
            WhatsAppTemplate = form.WhatsAppTemplate,
            EnableCaptcha = form.EnableCaptcha,
            AntiSpamEnabled = form.AntiSpamEnabled,
            AntiSpamProvider = form.AntiSpamProvider,
            AntiSpamSiteKey = null,
            AntiSpamSecretKey = null,
            SimpleCaptchaExpected = form.SimpleCaptchaExpected,
            Fields = MapFields(form),
            Versions = form.Versions
                .Select(v => new FormVersionItemViewModel
                {
                    Id = v.Id,
                    VersionNumber = v.VersionNumber,
                    State = v.State.ToString(),
                    CreatedAtUtc = v.CreatedAtUtc,
                    UpdatedAtUtc = v.UpdatedAtUtc,
                    IsPublishedPointer = v.IsPublishedPointer,
                    IsDraftPointer = v.IsDraftPointer
                })
                .ToList()
        });
        return model;
    }

    private async Task<int> CountSubmissionsAsync(Guid formId, CancellationToken cancellationToken)
    {
        var page = await _submissions.ListPagedAsync(
            new SubmissionListRequest { FormId = formId, Page = 1, PageSize = 1 },
            cancellationToken);
        return page.TotalCount;
    }

    private static string NormalizeTab(string? tab)
    {
        var value = string.IsNullOrWhiteSpace(tab) ? "general" : tab.Trim().ToLowerInvariant();
        return value switch
        {
            "general" or "fields" or "submit" or "actions" or "security" or "advanced" or "responses" => value,
            "aftersubmit" or "after-submit" => "submit",
            _ => "general"
        };
    }

    private static IReadOnlyList<FormFieldItemViewModel> MapFields(FormDetailDto form) =>
        form.Fields.Select(f => new FormFieldItemViewModel
        {
            Id = f.Id,
            Key = f.Key,
            Label = f.Label,
            FieldType = f.FieldType.ToString(),
            IsRequired = f.IsRequired,
            OptionsCsv = f.OptionsCsv,
            Placeholder = f.Placeholder,
            HelpText = f.HelpText,
            SortOrder = f.SortOrder,
            IsSystem = f.IsSystem
        }).ToList();

    private void AddValidationErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
        {
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
        }
    }
}
