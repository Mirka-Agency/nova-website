using CMS.Application.Common.Features;
using CMS.Modules.Forms.Application.Fields;
using CMS.Modules.Forms.Application.Interfaces;
using CMS.Modules.Forms.Application.Public;
using CMS.Modules.Forms.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Forms.Web.ViewComponents;

public sealed class FormEmbedViewComponent : ViewComponent
{
    private readonly IFormService _forms;
    private readonly IFormFieldTypeRegistry _fieldTypes;
    private readonly IFeatureManager _features;

    public FormEmbedViewComponent(
        IFormService forms,
        IFormFieldTypeRegistry fieldTypes,
        IFeatureManager features)
    {
        _forms = forms;
        _fieldTypes = fieldTypes;
        _features = features;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        string? slug = null,
        string? key = null,
        Guid? id = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return Content(string.Empty);

        FormPublicContract? form = null;
        if (id.HasValue)
            form = await _forms.GetPublicContractByIdAsync(id.Value, cancellationToken);
        else if (!string.IsNullOrWhiteSpace(key))
            form = await _forms.GetPublicContractByKeyAsync(key, cancellationToken);
        else if (!string.IsNullOrWhiteSpace(slug))
            form = await _forms.GetPublicContractBySlugAsync(slug, cancellationToken);

        if (form is null)
            return Content(string.Empty);

        return View(Map(form));
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
