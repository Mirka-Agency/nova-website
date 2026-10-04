using CMS.Application.Seo;
using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Domain.Entities;

namespace CMS.Modules.Seo.Infrastructure.Services;

public sealed class RobotsTxtBuilder : IRobotsTxtBuilder
{
    private readonly ISeoSiteSettingsService _settings;

    public RobotsTxtBuilder(ISeoSiteSettingsService settings)
    {
        _settings = settings;
    }

    public async Task<string> BuildAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        _ = baseUrl;
        var seo = await _settings.GetAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(seo.RobotsTxt))
            return SeoSiteSettings.NormalizeRobotsTxt(seo.RobotsTxt);

        return SeoSiteSettings.NormalizeRobotsTxt(SeoSiteSettings.DefaultRobotsTxt);
    }
}
