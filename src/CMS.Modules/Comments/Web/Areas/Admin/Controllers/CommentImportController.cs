using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Comments.Application.Import;
using CMS.Modules.Comments.Domain.Enums;
using CMS.Modules.Comments.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Comments.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewComments")]
public class CommentImportController : Controller
{
    private readonly ICommentImportService _import;
    private readonly ICommentTargetCatalog _catalog;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<CommentsAdmin> _localizer;
    private readonly CommentImportOptions _options;
    private readonly IMemoryCache _cache;

    public CommentImportController(
        ICommentImportService import,
        ICommentTargetCatalog catalog,
        IFeatureManager features,
        IStringLocalizer<CommentsAdmin> localizer,
        IOptions<CommentImportOptions> options,
        IMemoryCache cache)
    {
        _import = import;
        _catalog = catalog;
        _features = features;
        _localizer = localizer;
        _options = options.Value;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        ViewData["Title"] = _localizer["ImportComments"].Value;
        return View(new CommentImportUploadViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> SampleXml(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        return File(_import.GetSampleXml(), "application/xml", "wordpress-comments-with-page-url.xml");
    }

    [HttpPost]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    public async Task<IActionResult> Preview(CommentImportUploadViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        ViewData["Title"] = _localizer["ImportCommentsPreview"].Value;

        if (model.File is null || model.File.Length == 0)
        {
            ModelState.AddModelError(nameof(model.File), _localizer["ImportFileRequired"].Value);
            return View("Index", model);
        }

        if (!model.File.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(model.File.ContentType, "text/xml", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(model.File.ContentType, "application/xml", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.File), _localizer["ImportFileMustBeXml"].Value);
            return View("Index", model);
        }

        if (model.File.Length > _options.MaxUploadBytes)
        {
            ModelState.AddModelError(nameof(model.File), _localizer["ImportFileTooLarge"].Value);
            return View("Index", model);
        }

        try
        {
            await using var stream = model.File.OpenReadStream();
            var preview = await _import.BuildPreviewAsync(stream, cancellationToken);
            return RedirectToAction(nameof(Preview), new { id = preview.PreviewId, page = 1 });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Index", model);
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, _localizer["ImportParseFailed"].Value);
            return View("Index", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Preview(string id, int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        var preview = await _import.GetPreviewAsync(id, cancellationToken);
        if (preview is null)
        {
            TempData["Error"] = _localizer["ImportPreviewExpired"].Value;
            return RedirectToAction(nameof(Index));
        }

        ViewData["Title"] = _localizer["ImportCommentsPreview"].Value;
        return View(await ToPreviewViewModelAsync(preview, page, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(CommentImportConfirmFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        ViewData["Title"] = _localizer["ImportCommentsResult"].Value;

        try
        {
            var overrides = new List<CommentImportManualOverride>();
            foreach (var item in model.Overrides ?? [])
            {
                if (string.IsNullOrWhiteSpace(item.ExternalId) || string.IsNullOrWhiteSpace(item.ManualTargetKey))
                    continue;

                if (!TryParseTargetKey(item.ManualTargetKey, out var targetType, out var targetId))
                    continue;

                var candidate = await _catalog.GetAsync(targetType, targetId, cancellationToken);
                if (candidate is null)
                    continue;

                overrides.Add(new CommentImportManualOverride(
                    item.ExternalId.Trim(),
                    candidate.TargetType,
                    candidate.TargetId,
                    candidate.Title));
            }

            var result = await _import.ConfirmAsync(
                new CommentImportConfirmCommand(model.PreviewId, model.OnlyNew, overrides),
                cancellationToken);

            _cache.Set(ResultCacheKey(result.PreviewId), result, TimeSpan.FromHours(2));

            if (result.ImportedCount == 0)
            {
                TempData["Error"] = result.FailedCount > 0
                    ? _localizer["ImportNothingSavedFailed"].Value
                    : _localizer["ImportNothingSaved"].Value;
            }
            else
            {
                TempData["Success"] = string.Format(
                    _localizer["ImportSavedCount"].Value,
                    result.ImportedCount);
            }

            return View("Result", ToResultViewModel(result));
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadCsv(string id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        if (!_cache.TryGetValue(ResultCacheKey(id), out CommentImportResult? result) || result is null)
        {
            TempData["Error"] = _localizer["ImportReportExpired"].Value;
            return RedirectToAction(nameof(Index));
        }

        var bytes = _import.BuildCsvReport(result);
        return File(bytes, "text/csv", $"comment-import-{id}.csv");
    }

    private async Task<CommentImportPreviewViewModel> ToPreviewViewModelAsync(
        CommentImportPreview preview,
        int page,
        CancellationToken cancellationToken)
    {
        var targets = await _catalog.ListAsync(cancellationToken);
        var options = targets
            .OrderBy(t => t.TargetType)
            .ThenBy(t => t.Title)
            .Select(t => new SelectListItem(
                $"{TargetLabel(t.TargetType)} — {t.Title}",
                $"{(int)t.TargetType}:{t.TargetId}"))
            .ToList();

        options.Insert(0, new SelectListItem(_localizer["ImportSelectTarget"].Value, string.Empty));

        var pageSize = CommentImportPreviewViewModel.PageSize;
        var totalPages = Math.Max(1, (int)Math.Ceiling(preview.Rows.Count / (double)pageSize));
        var currentPage = page < 1 ? 1 : Math.Min(page, totalPages);
        var pageRows = preview.Rows
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new CommentImportPreviewRowViewModel
            {
                ExternalId = r.ExternalId,
                ExternalParentId = r.ExternalParentId,
                AuthorName = r.AuthorName,
                BodyPreview = r.Body.Length > 120 ? r.Body[..120] + "…" : r.Body,
                PublishedAtUtc = r.PublishedAtUtc,
                Status = r.Status,
                PageUrl = r.PageUrl,
                NormalizedPath = r.NormalizedPath,
                Disposition = r.Disposition,
                MatchedTargetType = r.MatchedTargetType,
                MatchedTargetId = r.MatchedTargetId,
                MatchedTargetTitle = r.MatchedTargetTitle,
                ErrorMessage = r.ErrorMessage,
                ManualTargetKey = r.MatchedTargetType is not null && r.MatchedTargetId is not null
                    ? $"{(int)r.MatchedTargetType}:{(Guid)r.MatchedTargetId}"
                    : null
            })
            .ToList();

        return new CommentImportPreviewViewModel
        {
            PreviewId = preview.PreviewId,
            TotalCount = preview.TotalCount,
            ReadyCount = preview.ReadyCount,
            DuplicateCount = preview.DuplicateCount,
            UnmatchedCount = preview.UnmatchedCount,
            AmbiguousCount = preview.AmbiguousCount,
            InvalidCount = preview.InvalidCount,
            OnlyNew = true,
            Page = currentPage,
            TotalPages = totalPages,
            TargetOptions = options,
            Rows = pageRows
        };
    }

    private static CommentImportResultViewModel ToResultViewModel(CommentImportResult result) =>
        new()
        {
            PreviewId = result.PreviewId,
            TotalCount = result.TotalCount,
            ImportedCount = result.ImportedCount,
            DuplicateCount = result.DuplicateCount,
            UnmatchedCount = result.UnmatchedCount,
            FailedCount = result.FailedCount,
            Queued = result.Queued,
            Rows = result.Rows.Select(r => new CommentImportResultRowViewModel
            {
                ExternalId = r.ExternalId,
                PageUrl = r.PageUrl,
                Outcome = r.Outcome,
                Message = r.Message,
                TargetTitle = r.TargetTitle
            }).ToList()
        };

    private static bool TryParseTargetKey(string key, out CommentTargetType targetType, out Guid targetId)
    {
        targetType = default;
        targetId = default;
        var parts = key.Split(':', 2);
        if (parts.Length != 2)
            return false;
        if (!int.TryParse(parts[0], out var typeValue) || !Enum.IsDefined(typeof(CommentTargetType), typeValue))
            return false;
        if (!Guid.TryParse(parts[1], out targetId))
            return false;
        targetType = (CommentTargetType)typeValue;
        return true;
    }

    private string TargetLabel(CommentTargetType type) => type switch
    {
        CommentTargetType.BlogPost => _localizer["BlogComments"].Value,
        CommentTargetType.Event => _localizer["EventComments"].Value,
        CommentTargetType.Product => _localizer["ProductComments"].Value,
        CommentTargetType.ProductCategory => _localizer["ProductCategoryComments"].Value,
        _ => type.ToString()
    };

    private static string ResultCacheKey(string previewId) => "comments:import-result:" + previewId;
}
