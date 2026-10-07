using CMS.Application.Common.Features;
using CMS.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Services.Web.Areas.Admin.Controllers;

/// <summary>CKEditor Simple Upload Adapter endpoint for inline blog images.</summary>
[Area("Admin")]
[Authorize(Policy = "ViewServices")]
[Route("admin/servicesmedia")]
public class ServicesMediaController : Controller
{
    private readonly IObjectStorage _objectStorage;
    private readonly IFeatureManager _features;
    private readonly ILogger<ServicesMediaController> _logger;

    public ServicesMediaController(
        IObjectStorage objectStorage,
        IFeatureManager features,
        ILogger<ServicesMediaController> logger)
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
        if (!await _features.IsEnabledAsync(FeatureNames.Services))
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
                ObjectStorageKeys.Modules.Services,
                "content",
                upload.FileName);

            var uploaded = await _objectStorage.UploadAsync(
                buffer,
                objectKey,
                upload.ContentType,
                cancellationToken);

            _logger.LogInformation("Admin action: uploaded services content image {ObjectKey}", uploaded.ObjectKey);
            return Ok(new { url = uploaded.PublicUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "services content image upload failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = new { message = "آپلود ناموفق بود." } });
        }
    }
}
