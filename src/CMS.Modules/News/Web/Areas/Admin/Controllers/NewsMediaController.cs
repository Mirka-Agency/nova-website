using CMS.Application.Common.Features;
using CMS.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.News.Web.Areas.Admin.Controllers;

/// <summary>CKEditor Simple Upload Adapter endpoint for inline news images.</summary>
[Area("Admin")]
[Authorize(Policy = "ViewNews")]
[Route("admin/newsmedia")]
public class NewsMediaController : Controller
{
    private readonly IObjectStorage _objectStorage;
    private readonly IFeatureManager _features;
    private readonly ILogger<NewsMediaController> _logger;

    public NewsMediaController(
        IObjectStorage objectStorage,
        IFeatureManager features,
        ILogger<NewsMediaController> logger)
    {
        _objectStorage = objectStorage;
        _features = features;
        _logger = logger;
    }

    [HttpPost("Upload")]
    [RequestSizeLimit(ImageUploadRules.MaxBytes + 1_048_576)]
    [RequestFormLimits(MultipartBodyLengthLimit = ImageUploadRules.MaxBytes + 1_048_576)]
    public async Task<IActionResult> Upload(IFormFile? upload, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        if (upload is null || upload.Length == 0)
        {
            return BadRequest(new { error = new { message = "فایلی آپلود نشده است." } });
        }

        try
        {
            await using var buffer = new MemoryStream();
            await upload.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            if (!ImageUploadRules.Validate(buffer, upload.ContentType, buffer.Length))
            {
                return BadRequest(new { error = new { message = "فقط تصاویر JPEG، PNG، WebP یا GIF معتبر مجاز هستند." } });
            }

            buffer.Position = 0;
            var objectKey = ObjectStorageKeys.Create(
                ObjectStorageKeys.Modules.News,
                "content",
                upload.FileName);

            var uploaded = await _objectStorage.UploadAsync(
                buffer,
                objectKey,
                upload.ContentType,
                cancellationToken);

            _logger.LogInformation("Admin action: uploaded news content image {ObjectKey}", uploaded.ObjectKey);
            return Ok(new { url = uploaded.PublicUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "News content image upload failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = new { message = "آپلود ناموفق بود." } });
        }
    }

    [HttpPost("UploadAttachment")]
    [RequestSizeLimit(FormFileUploadRules.MaxBytes + 1_048_576)]
    [RequestFormLimits(MultipartBodyLengthLimit = FormFileUploadRules.MaxBytes + 1_048_576)]
    public async Task<IActionResult> UploadAttachment(IFormFile? file, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        if (file is null || file.Length == 0)
            return BadRequest(new { error = new { message = "فایلی آپلود نشده است." } });

        try
        {
            await using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            if (!FormFileUploadRules.Validate(buffer, file.ContentType, buffer.Length))
            {
                return BadRequest(new
                {
                    error = new
                    {
                        message = "فقط فایل‌های PDF، Word، تصویر یا متن با حداکثر ۱۰ مگابایت مجاز هستند."
                    }
                });
            }

            buffer.Position = 0;
            var objectKey = ObjectStorageKeys.Create(
                ObjectStorageKeys.Modules.News,
                "attachments",
                file.FileName);

            var uploaded = await _objectStorage.UploadAsync(
                buffer,
                objectKey,
                file.ContentType,
                cancellationToken);

            var fileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "file";
            if (fileName.Length > 300)
                fileName = fileName[..300];

            _logger.LogInformation("Admin action: uploaded news article attachment {ObjectKey}", uploaded.ObjectKey);
            return Ok(new { url = uploaded.PublicUrl, fileName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "News article attachment upload failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = new { message = "آپلود ناموفق بود." } });
        }
    }
}
