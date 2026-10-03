using System.Text.Json;
using System.Text.Json.Serialization;
using CMS.Application.Admin;
using CMS.Application.Common.Features;
using CMS.Application.Storage;
using CMS.Domain.Exceptions;
using CMS.Modules.Forms.Application.Fields;
using CMS.Modules.Forms.Application.Interfaces;
using CMS.Modules.Forms.Application.Public;
using CMS.Modules.Forms.Application.Submissions;
using CMS.Modules.Forms.Web;
using CMS.Modules.Forms.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Forms.Web.Controllers;

[Route("forms")]
public class PublicFormsController : Controller
{
    private static readonly JsonSerializerOptions SchemaJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IFormService _forms;
    private readonly ISubmissionService _submissions;
    private readonly IFormFieldTypeRegistry _fieldTypes;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<FormsPublic> _localizer;

    public PublicFormsController(
        IFormService forms,
        ISubmissionService submissions,
        IFormFieldTypeRegistry fieldTypes,
        IFeatureManager features,
        IStringLocalizer<FormsPublic> localizer)
    {
        _forms = forms;
        _submissions = submissions;
        _fieldTypes = fieldTypes;
        _features = features;
        _localizer = localizer;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Show(string slug, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        var contract = await _forms.GetPublicContractBySlugAsync(slug, cancellationToken);
        if (contract is null)
            return NotFound();

        ViewData["Title"] = contract.Name;
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Edit("Forms", contract.Id, "ویرایش فرم", "ManageForms");
        return View(Map(contract));
    }

    [HttpGet("by-id/{id:guid}")]
    public async Task<IActionResult> ShowById(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        var contract = await _forms.GetPublicContractByIdAsync(id, cancellationToken);
        if (contract is null)
            return NotFound();

        ViewData["Title"] = contract.Name;
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Edit("Forms", contract.Id, "ویرایش فرم", "ManageForms");
        return View(nameof(Show), Map(contract));
    }

    /// <summary>Theme-safe public schema JSON (no secrets / no action configs).</summary>
    [HttpGet("{slug}/schema")]
    [Produces("application/json")]
    public async Task<IActionResult> Schema(string slug, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        var contract = await _forms.GetPublicContractBySlugAsync(slug, cancellationToken);
        if (contract is null)
            return NotFound();

        return Json(contract, SchemaJsonOptions);
    }

    [HttpGet("key/{key}/schema")]
    [Produces("application/json")]
    public async Task<IActionResult> SchemaByKey(string key, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        var contract = await _forms.GetPublicContractByKeyAsync(key, cancellationToken);
        if (contract is null)
            return NotFound();

        return Json(contract, SchemaJsonOptions);
    }

    [HttpGet("by-id/{id:guid}/schema")]
    [Produces("application/json")]
    public async Task<IActionResult> SchemaById(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        var contract = await _forms.GetPublicContractByIdAsync(id, cancellationToken);
        if (contract is null)
            return NotFound();

        return Json(contract, SchemaJsonOptions);
    }

    [HttpPost("{slug}")]
    [EnableRateLimiting("forms-submit")]
    [RequestSizeLimit(FormFileUploadRules.MaxBytes + 2_097_152)]
    [RequestFormLimits(MultipartBodyLengthLimit = FormFileUploadRules.MaxBytes + 2_097_152)]
    public async Task<IActionResult> Submit(string slug, PublicFormSubmitViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        var contract = await _forms.GetPublicContractBySlugAsync(slug, cancellationToken);
        if (contract is null)
            return NotFound();

        model = Remap(model, contract);
        ViewData["Title"] = contract.Name;

        foreach (var field in contract.Fields.Where(f =>
                     string.Equals(f.Type, FormFieldTypeIds.CheckboxGroup, StringComparison.OrdinalIgnoreCase)))
        {
            var values = Request.Form[$"Values[{field.Key}]"]
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .ToList();
            model.Values[field.Key] = values.Count == 0 ? null : string.Join('|', values);
        }

        var files = new List<SubmittedFile>();
        foreach (var field in contract.Fields.Where(f =>
                     string.Equals(f.Type, FormFieldTypeIds.File, StringComparison.OrdinalIgnoreCase)))
        {
            if (Request.Form.Files[field.Key] is { Length: > 0 } upload)
            {
                files.Add(new SubmittedFile(
                    field.Key,
                    upload.FileName,
                    upload.ContentType,
                    upload.Length,
                    upload.OpenReadStream()));
            }
        }

        var wantsJson = WantsJsonResponse();

        try
        {
            var result = await _submissions.SubmitAsync(new SubmitFormCommand(
                contract.Slug,
                model.Values,
                files,
                model.Website,
                model.CaptchaAnswer,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                BuildContext(),
                model.AntiSpamToken), cancellationToken);

            if (wantsJson)
            {
                return Json(new
                {
                    ok = true,
                    message = string.IsNullOrWhiteSpace(result.SuccessMessage)
                        ? _localizer["SubmitSuccess"].Value
                        : result.SuccessMessage,
                    redirectUrl = string.IsNullOrWhiteSpace(result.RedirectUrl) ? null : result.RedirectUrl.Trim()
                });
            }

            if (!string.IsNullOrWhiteSpace(result.RedirectUrl))
            {
                var target = result.RedirectUrl.Trim();
                if (target.StartsWith('/') && !target.StartsWith("//", StringComparison.Ordinal))
                    return LocalRedirect(target);

                if (Uri.TryCreate(target, UriKind.Absolute, out var absolute)
                    && absolute.Scheme is "http" or "https")
                    return Redirect(target);
            }

            TempData["Success"] = string.IsNullOrWhiteSpace(result.SuccessMessage)
                ? _localizer["SubmitSuccess"].Value
                : result.SuccessMessage;
            return RedirectToAction(nameof(Show), new { slug = contract.Slug });
        }
        catch (ValidationException ex)
        {
            foreach (var (key, messages) in ex.Errors)
            {
                foreach (var message in messages)
                    ModelState.AddModelError(key.StartsWith("Values[", StringComparison.Ordinal) ? key : $"Values[{key}]", message);
            }

            if (wantsJson)
            {
                var errors = ex.Errors.ToDictionary(
                    kv => kv.Key.StartsWith("Values[", StringComparison.Ordinal) ? kv.Key : $"Values[{kv.Key}]",
                    kv => kv.Value);
                return BadRequest(new { ok = false, errors });
            }

            return View(nameof(Show), model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            if (wantsJson)
                return BadRequest(new { ok = false, message = ex.Message, errors = new Dictionary<string, string[]> { [""] = [ex.Message] } });
            return View(nameof(Show), model);
        }
    }

    private bool WantsJsonResponse()
    {
        if (string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            return true;

        var accept = Request.Headers.Accept.ToString();
        return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private SubmissionContextDocument BuildContext()
    {
        string? Query(string name) =>
            Request.Query.TryGetValue(name, out var v) && !string.IsNullOrWhiteSpace(v)
                ? v.ToString().Trim()
                : null;

        var acceptLanguage = Request.Headers.AcceptLanguage.ToString();
        var locale = string.IsNullOrWhiteSpace(acceptLanguage)
            ? null
            : acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

        var referrer = Request.Headers.Referer.ToString();
        if (string.IsNullOrWhiteSpace(referrer))
            referrer = null;

        return new SubmissionContextDocument
        {
            Page = Request.Path.HasValue ? Request.Path.Value : null,
            Locale = locale,
            Referrer = referrer,
            UtmSource = Query("utm_source"),
            UtmMedium = Query("utm_medium"),
            UtmCampaign = Query("utm_campaign"),
            UtmTerm = Query("utm_term"),
            UtmContent = Query("utm_content")
        };
    }

    private PublicFormSubmitViewModel Map(FormPublicContract form) =>
        new()
        {
            Id = form.Id,
            Key = form.Key,
            Slug = form.Slug,
            Name = form.Name,
            Description = form.Description,
            PublishedVersionId = form.VersionId,
            SubmitButtonText = form.Settings.SubmitButtonText,
            SubmitBehaviorType = form.SubmitBehavior.Type,
            SuccessMessage = form.SubmitBehavior.Message,
            RedirectUrl = form.SubmitBehavior.Url,
            EnableCaptcha = string.Equals(form.AntiSpam.Provider, "simple_captcha", StringComparison.OrdinalIgnoreCase),
            AntiSpamEnabled = form.AntiSpam.Enabled,
            AntiSpamProvider = form.AntiSpam.Provider,
            AntiSpamSiteKey = form.AntiSpam.SiteKey,
            SchemaUrl = $"/forms/{form.Slug}/schema",
            Fields = form.Fields.Select(MapField).ToList(),
            Values = form.Fields.ToDictionary(
                f => f.Key,
                f => f.DefaultValue,
                StringComparer.OrdinalIgnoreCase)
        };

    private PublicFormSubmitViewModel Remap(PublicFormSubmitViewModel model, FormPublicContract form)
    {
        model.Id = form.Id;
        model.Key = form.Key;
        model.Slug = form.Slug;
        model.Name = form.Name;
        model.Description = form.Description;
        model.PublishedVersionId = form.VersionId;
        model.SubmitButtonText = form.Settings.SubmitButtonText;
        model.SubmitBehaviorType = form.SubmitBehavior.Type;
        model.SuccessMessage = form.SubmitBehavior.Message;
        model.RedirectUrl = form.SubmitBehavior.Url;
        model.EnableCaptcha = string.Equals(form.AntiSpam.Provider, "simple_captcha", StringComparison.OrdinalIgnoreCase);
        model.AntiSpamEnabled = form.AntiSpam.Enabled;
        model.AntiSpamProvider = form.AntiSpam.Provider;
        model.AntiSpamSiteKey = form.AntiSpam.SiteKey;
        model.SchemaUrl = $"/forms/{form.Slug}/schema";
        model.Fields = form.Fields.Select(f =>
        {
            var mapped = MapField(f);
            mapped.Value = model.Values.TryGetValue(f.Key, out var v) ? v : f.DefaultValue;
            return mapped;
        }).ToList();
        model.Values ??= new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        return model;
    }

    private PublicFormFieldViewModel MapField(FormPublicField field)
    {
        var legacy = _fieldTypes.ToLegacyEnum(field.Type) ?? Domain.Enums.FormFieldType.Text;
        return new PublicFormFieldViewModel
        {
            Id = field.Id,
            Key = field.Key,
            Label = field.Label,
            TypeId = field.Type,
            FieldType = legacy,
            IsRequired = field.Required,
            Placeholder = field.Placeholder,
            HelpText = field.HelpText,
            LayoutWidth = field.LayoutWidth,
            DefaultValue = field.DefaultValue,
            AllowedExtensions = field.Validation?.AllowedExtensions,
            MaxFileSizeBytes = field.Validation?.MaxFileSize,
            Options = field.Options
                .Select(o => new PublicFormOptionViewModel { Value = o.Value, Label = o.Label })
                .ToList(),
            Visibility = field.Visibility
        };
    }
}
