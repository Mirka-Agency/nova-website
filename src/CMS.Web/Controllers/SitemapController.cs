using System.Text;
using CMS.Application.Seo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CMS.Web.Controllers;

[AllowAnonymous]
[DisableRateLimiting]
public sealed class SitemapController : Controller
{
    private readonly ISitemapService _sitemap;
    private readonly IRobotsTxtBuilder _robots;

    public SitemapController(ISitemapService sitemap, IRobotsTxtBuilder robots)
    {
        _sitemap = sitemap;
        _robots = robots;
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> SitemapIndex(CancellationToken cancellationToken)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
        var xml = await _sitemap.BuildIndexXmlAsync(baseUrl, cancellationToken);
        return Content(xml, "application/xml", Encoding.UTF8);
    }

    [HttpGet("/sitemaps/{segment}")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> SitemapSegment(string segment, CancellationToken cancellationToken)
    {
        if (!IsValidSegment(segment))
            return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
        var xml = await _sitemap.BuildSegmentXmlAsync(segment, baseUrl, cancellationToken);
        if (xml is null)
            return NotFound();

        return Content(xml, "application/xml", Encoding.UTF8);
    }

    private static bool IsValidSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment) || segment.Length > 64)
            return false;

        foreach (var ch in segment)
        {
            if (char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_')
                continue;

            return false;
        }

        return true;
    }

    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<IActionResult> Robots(CancellationToken cancellationToken)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host.Value}".TrimEnd('/');
        var body = await _robots.BuildAsync(baseUrl, cancellationToken);
        return Content(body, "text/plain", Encoding.UTF8);
    }
}
