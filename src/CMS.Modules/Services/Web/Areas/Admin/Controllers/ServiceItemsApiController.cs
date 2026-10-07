using CMS.Application.Common.Features;
using CMS.Application.Editing;
using CMS.Application.Seo;
using CMS.Application.Users;
using CMS.Domain.Exceptions;
using CMS.Modules.Services.Application.Interfaces;
using CMS.Modules.Services.Application.ServiceItems;
using CMS.Modules.Services.Web.Areas.Admin.ViewModels;
using CMS.Modules.Seo.Application.Documents;
using CMS.Modules.Seo.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Services.Web.Areas.Admin.Controllers;

/// <summary>JSON API for creating/updating service items.</summary>
[ApiController]
[Authorize(Policy = "ViewServices")]
[Route("api/v1/admin/services/items")]
public class ServiceItemsApiController : ControllerBase
{
    private readonly IServiceItemService _posts;
    private readonly IContentAuthorLookup _authors;
    private readonly IFeatureManager _features;
    private readonly IAdminEditLockAccessor _editLocks;
    private readonly ISeoDocumentService _seoDocuments;
    private readonly ILogger<ServiceItemsApiController> _logger;

    public ServiceItemsApiController(
        IServiceItemService posts,
        IContentAuthorLookup authors,
        IFeatureManager features,
        IAdminEditLockAccessor editLocks,
        ISeoDocumentService seoDocuments,
        ILogger<ServiceItemsApiController> logger)
    {
        _posts = posts;
        _authors = authors;
        _features = features;
        _editLocks = editLocks;
        _seoDocuments = seoDocuments;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "ManageServices")]
    [ProducesResponseType(typeof(SaveServiceItemApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] SaveServiceItemApiRequest request, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Services))
            return NotFound();

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var id = await _posts.CreateAsync(await ToCommandAsync(request, cancellationToken), cancellationToken);
            var detail = await _posts.GetAsync(id, cancellationToken);
            _logger.LogInformation("Admin API: created service item {PostId}", id);
            return CreatedAtAction(
                nameof(Get),
                new { id },
                new SaveServiceItemApiResponse { Id = id, Slug = detail?.Slug ?? string.Empty });
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
    [Authorize(Policy = "ManageServices")]
    [ProducesResponseType(typeof(SaveServiceItemApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveServiceItemApiRequest request, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Services))
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
            await _posts.UpdateAsync(id, await ToCommandAsync(request, cancellationToken), cancellationToken);
            await UpsertSeoAsync(id, request, cancellationToken);
            var detail = await _posts.GetAsync(id, cancellationToken);
            _logger.LogInformation("Admin API: updated service item {PostId}", id);
            return Ok(new SaveServiceItemApiResponse { Id = id, Slug = detail?.Slug ?? string.Empty });
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
    [ProducesResponseType(typeof(ServiceItemDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Services))
            return NotFound();

        var post = await _posts.GetAsync(id, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    private async Task<SaveServiceItemCommand> ToCommandAsync(SaveServiceItemApiRequest request, CancellationToken cancellationToken)
    {
        var authorUserId = string.IsNullOrWhiteSpace(request.AuthorUserId) ? null : request.AuthorUserId.Trim();
        string? authorDisplayName = string.IsNullOrWhiteSpace(request.AuthorDisplayName)
            ? null
            : request.AuthorDisplayName.Trim();

        if (authorUserId is not null && authorDisplayName is null)
            authorDisplayName = await _authors.GetDisplayNameAsync(authorUserId, cancellationToken);

        return new(
            string.IsNullOrWhiteSpace(request.Title) ? ServiceItemDraftDefaults.Title : request.Title.Trim(),
            request.Slug,
            request.Body ?? string.Empty,
            request.Excerpt,
            request.CategoryId,
            request.CoverImageUrl,
            request.CoverImageAlt,
            request.Publish,
            request.PublishedAtUtc,
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

    private async Task UpsertSeoAsync(Guid id, SaveServiceItemApiRequest request, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return;

        await _seoDocuments.UpsertAsync(new SaveSeoDocumentCommand(
            SeoContentTypeKeys.ServiceItem,
            id,
            request.FocusKeyword,
            request.RobotsIndex,
            request.RobotsFollow,
            request.SchemaType,
            SeoScore: request.SeoScore), cancellationToken);
    }

    private async Task<bool> EnsureEditLockAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await _editLocks.CurrentUserHoldsAsync(EditLockEntityTypes.ServiceItem, id, cancellationToken: cancellationToken))
            return true;

        var acquired = await _editLocks.TryAcquireForCurrentUserAsync(
            EditLockEntityTypes.ServiceItem, id, cancellationToken);
        return acquired?.Acquired == true;
    }
}
