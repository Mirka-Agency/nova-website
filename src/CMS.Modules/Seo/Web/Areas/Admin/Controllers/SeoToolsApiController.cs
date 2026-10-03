using CMS.Application.Common.Features;
using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Application.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Seo.Web.Areas.Admin.Controllers;

[ApiController]
[Authorize(Policy = "ViewSeo")]
[Route("api/v1/admin/seo/tools")]
public class SeoToolsApiController : ControllerBase
{
    private readonly ISeoLinkSuggestionService _suggestions;
    private readonly IBrokenLinkChecker _brokenLinks;
    private readonly IFeatureManager _features;

    public SeoToolsApiController(
        ISeoLinkSuggestionService suggestions,
        IBrokenLinkChecker brokenLinks,
        IFeatureManager features)
    {
        _suggestions = suggestions;
        _brokenLinks = brokenLinks;
        _features = features;
    }

    [HttpPost("suggest-links")]
    [Authorize(Policy = "ManageSeo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SuggestLinks([FromBody] SuggestLinksRequest request, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return NotFound();

        var items = await _suggestions.SuggestAsync(
            request.Keyword,
            request.ExcludeContentType,
            request.ExcludeId,
            request.Take <= 0 ? 10 : request.Take,
            cancellationToken);
        return Ok(items);
    }

    [HttpPost("check-links")]
    [Authorize(Policy = "ManageSeo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckLinks([FromBody] CheckLinksRequest request, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return NotFound();

        var urls = request.Urls ?? [];
        var results = await _brokenLinks.CheckAsync(urls, cancellationToken);
        return Ok(results);
    }
}
