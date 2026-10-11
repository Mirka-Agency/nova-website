using CMS.Application.Common.Features;
using CMS.Application.Settings;
using CMS.Modules.Voices.Application.Interfaces;
using CMS.Modules.Voices.Application.VoiceItems;
using CMS.Modules.Voices.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Voices.Web.ViewComponents;

public sealed class TestimonialsViewComponent : ViewComponent
{
    private readonly IPublicVoiceItemQuery _voices;
    private readonly IFeatureManager _features;
    private readonly ISiteSettingsService _siteSettings;

    public TestimonialsViewComponent(
        IPublicVoiceItemQuery voices,
        IFeatureManager features,
        ISiteSettingsService siteSettings)
    {
        _voices = voices;
        _features = features;
        _siteSettings = siteSettings;
    }

    public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PublicVoiceItemDto> voices = Array.Empty<PublicVoiceItemDto>();
        if (await _features.IsEnabledAsync(FeatureNames.Voices))
            voices = await _voices.ListPublishedAsync(cancellationToken);

        var settings = await _siteSettings.GetAsync(cancellationToken);
        var brand = SiteContactInfo.From(settings).Brand;

        return View(new TestimonialsSectionViewModel
        {
            Brand = brand,
            Voices = voices
        });
    }
}
