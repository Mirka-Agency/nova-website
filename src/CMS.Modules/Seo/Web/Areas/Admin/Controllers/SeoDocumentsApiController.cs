using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Seo.Application.Documents;
using CMS.Modules.Seo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Seo.Web.Areas.Admin.Controllers;

[ApiController]
[Authorize(Policy = "ViewSeo")]
[Route("api/v1/admin/seo/documents")]
public class SeoDocumentsApiController : ControllerBase
{
    private readonly ISeoDocumentService _documents;
    private readonly IFeatureManager _features;
    private readonly ILogger<SeoDocumentsApiController> _logger;

    public SeoDocumentsApiController(
        ISeoDocumentService documents,
        IFeatureManager features,
        ILogger<SeoDocumentsApiController> logger)
    {
        _documents = documents;
        _features = features;
        _logger = logger;
    }

    [HttpGet("{contentType}/{contentId:guid}")]
    [ProducesResponseType(typeof(SeoDocumentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(string contentType, Guid contentId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return NotFound();

        var doc = await _documents.GetAsync(contentType, contentId, cancellationToken);
        if (doc is null)
        {
            return Ok(new SeoDocumentDto(
                Guid.Empty,
                contentType,
                contentId,
                null,
                true,
                true,
                null,
                null,
                null,
                null,
                DateTime.UtcNow,
                null));
        }

        return Ok(doc);
    }

    [HttpPut("{contentType}/{contentId:guid}")]
    [Authorize(Policy = "ManageSeo")]
    [ProducesResponseType(typeof(SeoDocumentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Put(
        string contentType,
        Guid contentId,
        [FromBody] SaveSeoDocumentApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return NotFound();

        try
        {
            var dto = await _documents.UpsertAsync(new SaveSeoDocumentCommand(
                contentType,
                contentId,
                request.FocusKeyword,
                request.RobotsIndex,
                request.RobotsFollow,
                request.SchemaType,
                request.SchemaJson,
                request.SeoScore,
                request.AnalysisJson), cancellationToken);
            _logger.LogInformation("Admin API: upserted SEO document {ContentType}/{ContentId}", contentType, contentId);
            return Ok(dto);
        }
        catch (ValidationException ex)
        {
            var problem = new ValidationProblemDetails();
            foreach (var (key, messages) in ex.Errors)
                problem.Errors[key] = messages;
            return ValidationProblem(problem);
        }
        catch (DomainException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "درخواست نامعتبر",
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
    }
}

public sealed class SaveSeoDocumentApiRequest
{
    public string? FocusKeyword { get; set; }
    public bool RobotsIndex { get; set; } = true;
    public bool RobotsFollow { get; set; } = true;
    public string? SchemaType { get; set; }
    public string? SchemaJson { get; set; }
    public int? SeoScore { get; set; }
    public string? AnalysisJson { get; set; }
}
