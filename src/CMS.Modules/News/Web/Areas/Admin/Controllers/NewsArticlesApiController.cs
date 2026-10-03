using CMS.Application.Common.Features;
using CMS.Application.Editing;
using CMS.Application.Seo;
using CMS.Application.Users;
using CMS.Domain.Exceptions;
using CMS.Modules.News.Application.Interfaces;
using CMS.Modules.News.Application.Articles;
using CMS.Modules.News.Web.Areas.Admin.ViewModels;
using CMS.Modules.Seo.Application.Documents;
using CMS.Modules.Seo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.News.Web.Areas.Admin.Controllers;

/// <summary>JSON API for creating/updating news articles.</summary>
[ApiController]
[Authorize(Policy = "ViewNews")]
[Route("api/v1/admin/news/articles")]
public class NewsArticlesApiController : ControllerBase
{
    private readonly IArticleService _articles;
    private readonly IContentAuthorLookup _authors;
    private readonly IFeatureManager _features;
    private readonly IAdminEditLockAccessor _editLocks;
    private readonly ISeoDocumentService _seoDocuments;
    private readonly ILogger<NewsArticlesApiController> _logger;

    public NewsArticlesApiController(
        IArticleService posts,
        IContentAuthorLookup authors,
        IFeatureManager features,
        IAdminEditLockAccessor editLocks,
        ISeoDocumentService seoDocuments,
        ILogger<NewsArticlesApiController> logger)
    {
        _articles = posts;
        _authors = authors;
        _features = features;
        _editLocks = editLocks;
        _seoDocuments = seoDocuments;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "ManageNews")]
    [ProducesResponseType(typeof(SaveArticleApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] SaveArticleApiRequest request, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var id = await _articles.CreateAsync(await ToCommandAsync(request, cancellationToken), cancellationToken);
            var detail = await _articles.GetAsync(id, cancellationToken);
            _logger.LogInformation("Admin API: created news article {PostId}", id);
            return CreatedAtAction(
                nameof(Get),
                new { id },
                new SaveArticleApiResponse { Id = id, Slug = detail?.Slug ?? string.Empty });
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblem(ex));
        }
        catch (DomainException ex)
        {
            return DomainProblem(ex);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManageNews")]
    [ProducesResponseType(typeof(SaveArticleApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveArticleApiRequest request, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (!await EnsureEditLockAsync(id, cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "قفل ویرایش",
                Detail = "این مطلب توسط کاربر دیگری در حال ویرایش است.",
                Instance = HttpContext.Request.Path
            });
        }

        try
        {
            await _articles.UpdateAsync(id, await ToCommandAsync(request, cancellationToken), cancellationToken);
            await UpsertSeoAsync(id, request, cancellationToken);
            var detail = await _articles.GetAsync(id, cancellationToken);
            _logger.LogInformation("Admin API: updated news article {PostId}", id);
            return Ok(new SaveArticleApiResponse { Id = id, Slug = detail?.Slug ?? string.Empty });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblem(ex));
        }
        catch (DomainException ex)
        {
            return DomainProblem(ex);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ArticleDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        var post = await _articles.GetAsync(id, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    private async Task<SaveArticleCommand> ToCommandAsync(SaveArticleApiRequest request, CancellationToken cancellationToken)
    {
        var authorUserId = string.IsNullOrWhiteSpace(request.AuthorUserId) ? null : request.AuthorUserId.Trim();
        string? authorDisplayName = string.IsNullOrWhiteSpace(request.AuthorDisplayName)
            ? null
            : request.AuthorDisplayName.Trim();

        if (authorUserId is not null && authorDisplayName is null)
            authorDisplayName = await _authors.GetDisplayNameAsync(authorUserId, cancellationToken);

        return new(
            string.IsNullOrWhiteSpace(request.Title) ? ArticleDraftDefaults.Title : request.Title.Trim(),
            request.Slug,
            request.Body ?? string.Empty,
            request.Excerpt,
            request.Kind,
            request.CategoryId,
            request.CoverImageUrl,
            request.Publish,
            request.PublishedAtUtc,
            request.EventStartAtUtc,
            request.EventEndAtUtc,
            request.Location,
            authorUserId,
            authorDisplayName,
            request.MetaTitle,
            request.MetaDescription,
            request.SeoKeywords,
            request.CanonicalUrl,
            request.OgTitle,
            request.OgDescription,
            request.OgImageUrl);
    }

    private ObjectResult DomainProblem(DomainException ex)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "درخواست نامعتبر",
            Detail = ex.Message,
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return BadRequest(problem);
    }

    private static ValidationProblemDetails CreateValidationProblem(ValidationException ex)
    {
        var problem = new ValidationProblemDetails();
        foreach (var (key, messages) in ex.Errors)
            problem.Errors[key] = messages;
        return problem;
    }

    private async Task UpsertSeoAsync(Guid id, SaveArticleApiRequest request, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return;

        await _seoDocuments.UpsertAsync(new SaveSeoDocumentCommand(
            SeoContentTypeKeys.NewsArticle,
            id,
            request.FocusKeyword,
            request.RobotsIndex,
            request.RobotsFollow,
            request.SchemaType,
            SeoScore: request.SeoScore), cancellationToken);
    }

    private async Task<bool> EnsureEditLockAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await _editLocks.CurrentUserHoldsAsync(EditLockEntityTypes.NewsArticle, id, cancellationToken: cancellationToken))
            return true;

        var acquired = await _editLocks.TryAcquireForCurrentUserAsync(
            EditLockEntityTypes.NewsArticle, id, cancellationToken);
        return acquired?.Acquired == true;
    }
}
