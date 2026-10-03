using CMS.Application.Storage;
using CMS.Domain.Exceptions;
using CMS.Modules.Media.Application.Assets;
using CMS.Modules.Media.Application.Imaging;
using CMS.Modules.Media.Application.Interfaces;
using CMS.Modules.Media.Web.Areas.Admin.ViewModels;
using CMS.Modules.Media.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace CMS.Modules.Media.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewMedia")]
public class MediaController : Controller
{
    private readonly IMediaLibraryService _media;
    private readonly IAuthorizationService _authorization;
    private readonly IStringLocalizer<MediaAdmin> _localizer;

    public MediaController(
        IMediaLibraryService media,
        IAuthorizationService authorization,
        IStringLocalizer<MediaAdmin> localizer)
    {
        _media = media;
        _authorization = authorization;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(int page = 1, string? q = null, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = _localizer["MediaLibrary"].Value;
        var result = await _media.ListPagedAsync(
            new CMS.Application.Common.Paging.PagedRequest { Page = page, PageSize = 36, Search = q },
            cancellationToken);
        ViewBag.Page = result.Page;
        ViewBag.PageSize = result.PageSize;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.TotalPages = result.TotalPages;
        ViewBag.Search = q;
        ViewBag.CanManage = (await _authorization.AuthorizeAsync(User, "ManageMedia")).Succeeded;

        var model = result.Items.Select(a => new MediaListItemViewModel
        {
            Id = a.Id,
            FileName = a.FileName,
            Title = a.Title,
            AltText = a.AltText,
            ContentType = a.ContentType,
            SizeDisplay = FormatSize(a.SizeBytes),
            PublicUrl = a.PublicUrl,
            ThumbnailPublicUrl = a.ThumbnailPublicUrl ?? a.PublicUrl,
            CreatedAtUtc = a.CreatedAtUtc
        }).ToList();

        return View(model);
    }

    [HttpGet]
    public IActionResult Upload()
    {
        ViewData["Title"] = _localizer["UploadMedia"].Value;
        return View(new MediaUploadViewModel());
    }

    [HttpPost]
    [RequestSizeLimit(ImageUploadRules.MaxBytes + 1_048_576)]
    public async Task<IActionResult> Upload(MediaUploadViewModel model, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        ViewData["Title"] = _localizer["UploadMedia"].Value;
        if (model.File is null || model.File.Length == 0)
        {
            ModelState.AddModelError(nameof(model.File), _localizer["SelectFile"].Value);
            return View(model);
        }

        try
        {
            await using var stream = model.File.OpenReadStream();
            // Don't tie S3 upload lifetime to browser abort of the HTML form post
            // (RequestAborted cancels mid-upload and surfaces as TaskCanceledException/500).
            using var uploadCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
            uploadCts.CancelAfter(TimeSpan.FromMinutes(5));
            await _media.UploadAsync(
                new UploadMediaCommand(
                    stream,
                    model.File.FileName,
                    model.File.ContentType,
                    model.File.Length,
                    model.Title,
                    model.AltText,
                    Optimize: ResolveOptimizeOptions(model.MaxWidth, model.Quality)),
                uploadCts.Token);
            TempData["Success"] = _localizer["MediaUploaded"].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            foreach (var (key, messages) in ex.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(key, message);
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (OperationCanceledException)
        {
            ModelState.AddModelError(string.Empty, "آپلود لغو شد یا زمانش تمام شد. دوباره تلاش کنید.");
            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Content(
        Guid id,
        string? source,
        [FromServices] IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        var asset = await _media.GetAsync(id, cancellationToken);
        if (asset is null)
            return NotFound();

        var useOriginal = string.Equals(source, "original", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(asset.OriginalPublicUrl);
        var url = useOriginal ? asset.OriginalPublicUrl! : asset.PublicUrl;
        var contentType = useOriginal
            ? (asset.OriginalContentType ?? asset.ContentType)
            : asset.ContentType;

        try
        {
            var client = httpClientFactory.CreateClient("MediaContent");
            using var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return File(bytes, contentType, enableRangeProcessing: false);
        }
        catch
        {
            return BadRequest(new { message = "Unable to fetch image content." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var asset = await _media.GetAsync(id, cancellationToken);
        if (asset is null)
            return NotFound();

        return Json(MapAssetJson(asset));
    }

    [HttpGet]
    public async Task<IActionResult> Resolve([FromQuery] string? url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest(new { message = _localizer["SelectFile"].Value });

        var asset = await _media.FindByPublicUrlAsync(url, cancellationToken);
        if (asset is null)
            return NotFound(new { message = _localizer["ImageNotInLibrary"].Value });

        return Json(MapAssetJson(asset));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateMetadata(Guid id, MediaMetadataFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _media.UpdateMetadataAsync(
                id,
                new UpdateMediaMetadataCommand(model.Title, model.AltText, model.Caption, model.Description),
                cancellationToken);
            return Json(new { ok = true, message = _localizer["MediaUpdated"].Value });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [RequestSizeLimit(ImageUploadRules.MaxBytes + 1_048_576)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageUploadRules.MaxBytes + 1_048_576)]
    public async Task<IActionResult> ReplaceImage(Guid id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = _localizer["SelectFile"].Value });

        try
        {
            await using var stream = file.OpenReadStream();
            var updated = await _media.ReplaceFileAsync(
                id,
                new ReplaceMediaFileCommand(stream, file.FileName, file.ContentType, file.Length),
                cancellationToken);
            return Json(new
            {
                ok = true,
                message = _localizer["ImageEdited"].Value,
                publicUrl = updated.PublicUrl,
                thumbnailPublicUrl = updated.ThumbnailPublicUrl ?? updated.PublicUrl,
                fileName = updated.FileName,
                contentType = updated.ContentType,
                sizeDisplay = FormatSize(updated.SizeBytes),
                width = updated.Width,
                height = updated.Height,
                originalPublicUrl = updated.OriginalPublicUrl,
                originalSizeDisplay = updated.OriginalSizeBytes is long originalSize ? FormatSize(originalSize) : null,
                hasSeparateOriginal = updated.HasSeparateOriginal
            });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ValidationException ex)
        {
            var message = ex.Errors.SelectMany(e => e.Value).FirstOrDefault() ?? _localizer["InvalidImageType"].Value;
            return BadRequest(new { message });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _media.DeleteAsync(id, cancellationToken);
            if (Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { ok = true, message = _localizer["MediaDeleted"].Value });
            }

            TempData["Success"] = _localizer["MediaDeleted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RestoreOriginal(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var restored = await _media.RestoreOriginalAsync(id, cancellationToken);
            return Json(new
            {
                ok = true,
                message = _localizer["ImageRestored"].Value,
                publicUrl = restored.PublicUrl,
                thumbnailPublicUrl = restored.ThumbnailPublicUrl ?? restored.PublicUrl,
                fileName = restored.FileName,
                contentType = restored.ContentType,
                sizeDisplay = FormatSize(restored.SizeBytes),
                width = restored.Width,
                height = restored.Height,
                originalPublicUrl = restored.OriginalPublicUrl,
                originalSizeDisplay = restored.OriginalSizeBytes is long originalSize ? FormatSize(originalSize) : null,
                hasSeparateOriginal = restored.HasSeparateOriginal
            });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Picker(
        int page = 1,
        int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var result = await _media.ListPagedAsync(
            new CMS.Application.Common.Paging.PagedRequest { Page = page, PageSize = pageSize },
            cancellationToken);

        return Json(new
        {
            items = result.Items.Select(a => new
            {
                id = a.Id,
                fileName = a.FileName,
                title = a.Title,
                altText = a.AltText,
                publicUrl = a.PublicUrl,
                thumbnailUrl = a.ThumbnailPublicUrl ?? a.PublicUrl,
                contentType = a.ContentType
            }),
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            totalPages = result.TotalPages,
            hasNext = result.HasNext
        });
    }

    [HttpPost]
    [RequestSizeLimit(ImageUploadRules.MaxBytes + 1_048_576)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageUploadRules.MaxBytes + 1_048_576)]
    public async Task<IActionResult> UploadPicker(
        IFormFile? file,
        string? title,
        string? altText,
        string? maxWidth = null,
        string? quality = null,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = _localizer["SelectFile"].Value });

        try
        {
            await using var stream = file.OpenReadStream();
            using var uploadCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
            uploadCts.CancelAfter(TimeSpan.FromMinutes(5));
            var asset = await _media.UploadAsync(
                new UploadMediaCommand(
                    stream,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    title,
                    altText,
                    Optimize: ResolveOptimizeOptions(ParseOptionalInt(maxWidth), ParseOptionalInt(quality))),
                uploadCts.Token);

            return Json(new
            {
                ok = true,
                id = asset.Id,
                fileName = asset.FileName,
                title = asset.Title,
                altText = asset.AltText,
                publicUrl = asset.PublicUrl,
                thumbnailUrl = asset.ThumbnailPublicUrl ?? asset.PublicUrl,
                contentType = asset.ContentType
            });
        }
        catch (ValidationException ex)
        {
            var message = ex.Errors.SelectMany(e => e.Value).FirstOrDefault() ?? _localizer["InvalidImageType"].Value;
            return BadRequest(new { message });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (OperationCanceledException)
        {
            return BadRequest(new { message = "آپلود لغو شد یا زمانش تمام شد. دوباره تلاش کنید." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static MediaOptimizeOptions? ResolveOptimizeOptions(int? maxWidth, int? quality) =>
        MediaOptimizeOptions.FromOptional(maxWidth, quality);

    private static int? ParseOptionalInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return int.TryParse(value.Trim(), System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private object MapAssetJson(MediaAssetDetailDto asset) => new
    {
        id = asset.Id,
        fileName = asset.FileName,
        title = asset.Title,
        altText = asset.AltText,
        caption = asset.Caption,
        description = asset.Description,
        contentType = asset.ContentType,
        sizeBytes = asset.SizeBytes,
        sizeDisplay = FormatSize(asset.SizeBytes),
        publicUrl = asset.PublicUrl,
        thumbnailPublicUrl = asset.ThumbnailPublicUrl ?? asset.PublicUrl,
        width = asset.Width,
        height = asset.Height,
        originalFileName = asset.OriginalFileName,
        originalContentType = asset.OriginalContentType,
        originalSizeBytes = asset.OriginalSizeBytes,
        originalSizeDisplay = asset.OriginalSizeBytes is long originalSize ? FormatSize(originalSize) : null,
        originalPublicUrl = asset.OriginalPublicUrl,
        hasSeparateOriginal = asset.HasSeparateOriginal,
        variants = asset.Variants,
        createdAtUtc = asset.CreatedAtUtc,
        updatedAtUtc = asset.UpdatedAtUtc
    };

    private static string FormatSize(long bytes) =>
        bytes < 1024 ? $"{bytes} B"
        : bytes < 1024 * 1024 ? $"{bytes / 1024.0:0.#} KB"
        : $"{bytes / (1024.0 * 1024.0):0.##} MB";
}
