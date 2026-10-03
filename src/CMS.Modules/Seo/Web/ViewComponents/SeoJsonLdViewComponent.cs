using CMS.Application.Common.Features;
using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using System.Text.Json;

namespace CMS.Modules.Seo.Web.ViewComponents;

public sealed class SeoJsonLdViewComponent : ViewComponent
{
    private readonly ISeoSiteSettingsService _settings;
    private readonly IFeatureManager _features;

    public SeoJsonLdViewComponent(ISeoSiteSettingsService settings, IFeatureManager features)
    {
        _settings = settings;
        _features = features;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return Content(string.Empty);

        // Page-level schema takes precedence (set by controllers via ViewData).
        if (ViewContext.ViewData["SchemaJson"] is string pageSchema && !string.IsNullOrWhiteSpace(pageSchema))
            return View((object)pageSchema);

        var settings = await _settings.GetAsync();
        if (string.IsNullOrWhiteSpace(settings.OrganizationName))
            return Content(string.Empty);

        var schemaType = string.IsNullOrWhiteSpace(settings.DefaultSchemaType)
            ? SeoSchemaTypes.Organization
            : settings.DefaultSchemaType;

        var payload = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = schemaType,
            ["name"] = settings.OrganizationName
        };

        if (!string.IsNullOrWhiteSpace(settings.OrganizationUrl))
            payload["url"] = settings.OrganizationUrl;

        if (!string.IsNullOrWhiteSpace(settings.OrganizationLogoUrl))
            payload["logo"] = settings.OrganizationLogoUrl;

        var json = JsonSerializer.Serialize(payload);
        return View((object)json);
    }
}
