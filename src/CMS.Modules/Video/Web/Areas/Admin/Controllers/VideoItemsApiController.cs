using CMS.Application.Common.Features;
using CMS.Application.Editing;
using CMS.Application.Users;
using CMS.Domain.Exceptions;
using CMS.Modules.Video.Application.Interfaces;
using CMS.Modules.Video.Application.VideoItems;
using CMS.Modules.Video.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Video.Web.Areas.Admin.Controllers;

/// <summary>JSON API for creating/updating service items.</summary>
[ApiController]
[Authorize(Policy = "ViewVideo")]
[Route("api/v1/admin/videos/items")]
public class VideoItemsApiController : ControllerBase
{
    private readonly IVideoItemService _posts;
    private readonly IContentAuthorLookup _authors;
    private readonly IFeatureManager _features;
    private readonly IAdminEditLockAccessor _editLocks;
    private readonly ILogger<VideoItemsApiController> _logger;

    public VideoItemsApiController(
        IVideoItemService posts,
        IContentAuthorLookup authors,
        IFeatureManager features,
        IAdminEditLockAccessor editLocks,
        ILogger<VideoItemsApiController> logger)
    {
        _posts = posts;
        _authors = authors;
        _features = features;
        _editLocks = editLocks;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "ManageVideo")]
    [ProducesResponseType(typeof(SaveVideoItemApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] SaveVideoItemApiRequest request, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Video))
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
                new SaveVideoItemApiResponse { Id = id, Slug = detail?.Slug ?? string.Empty });
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
    [Authorize(Policy = "ManageVideo")]
    [ProducesResponseType(typeof(SaveVideoItemApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveVideoItemApiRequest request, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Video))
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
            var detail = await _posts.GetAsync(id, cancellationToken);
            _logger.LogInformation("Admin API: updated service item {PostId}", id);
            return Ok(new SaveVideoItemApiResponse { Id = id, Slug = detail?.Slug ?? string.Empty });
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
    [ProducesResponseType(typeof(VideoItemDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Video))
            return NotFound();

        var post = await _posts.GetAsync(id, cancellationToken);
        return post is null ? NotFound() : Ok(post);
    }

    private async Task<SaveVideoItemCommand> ToCommandAsync(SaveVideoItemApiRequest request, CancellationToken cancellationToken)
    {
        var authorUserId = string.IsNullOrWhiteSpace(request.AuthorUserId) ? null : request.AuthorUserId.Trim();
        string? authorDisplayName = string.IsNullOrWhiteSpace(request.AuthorDisplayName)
            ? null
            : request.AuthorDisplayName.Trim();

        if (authorUserId is not null && authorDisplayName is null)
            authorDisplayName = await _authors.GetDisplayNameAsync(authorUserId, cancellationToken);

        return new(
            string.IsNullOrWhiteSpace(request.Title) ? VideoItemDraftDefaults.Title : request.Title.Trim(),
            request.Slug,
            request.Body ?? string.Empty,
            request.Excerpt,
            request.VideoUrl,
            request.CategoryId,
            request.CoverImageUrl,
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

    private async Task<bool> EnsureEditLockAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await _editLocks.CurrentUserHoldsAsync(EditLockEntityTypes.VideoItem, id, cancellationToken: cancellationToken))
            return true;

        var acquired = await _editLocks.TryAcquireForCurrentUserAsync(
            EditLockEntityTypes.VideoItem, id, cancellationToken);
        return acquired?.Acquired == true;
    }
}
