using System.Text;
using CMS.Application.Seo;
using CMS.Modules.Seo.Application.Interfaces;

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
        var seo = await _settings.GetAsync(cancellationToken);
        var trimmedBase = baseUrl.TrimEnd('/');
        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");
        sb.AppendLine("Disallow: /forms/");
        sb.AppendLine();

        if (seo.SitemapEnabled)
            sb.AppendLine($"Sitemap: {trimmedBase}/sitemap.xml");

        if (!string.IsNullOrWhiteSpace(seo.RobotsTxtExtra))
        {
            sb.AppendLine();
            sb.AppendLine(seo.RobotsTxtExtra.Trim());
        }

        return sb.ToString();
    }
}
