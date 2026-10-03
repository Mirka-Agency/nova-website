using CMS.Application.Common.Features;
using CMS.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Voices.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewVoices")]
[Route("Admin/VoicesMedia")]
public class VoicesMediaController : Controller
{
    private readonly IObjectStorage _objectStorage;
    private readonly IFeatureManager _features;
    private readonly ILogger<VoicesMediaController> _logger;

    public VoicesMediaController(
        IObjectStorage objectStorage,
        IFeatureManager features,
        ILogger<VoicesMediaController> logger)
    {
        _objectStorage = objectStorage;
        _features = features;
        _logger = logger;
    }

    [HttpPost("UploadAudio")]
    [RequestSizeLimit(AudioUploadRules.MaxBytes + 1_048_576)]
    [RequestFormLimits(MultipartBodyLengthLimit = AudioUploadRules.MaxBytes + 1_048_576)]
    public async Task<IActionResult> UploadAudio(IFormFile? file, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Voices))
            return NotFound();

        if (file is null || file.Length == 0)
            return BadRequest(new { error = new { message = "فایلی آپلود نشده است." } });

        try
        {
            await using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            if (!AudioUploadRules.Validate(buffer, file.ContentType, buffer.Length))
            {
                return BadRequest(new
                {
                    error = new
                    {
                        message = "فقط فایل‌های صوتی MP3، M4A، AAC، OGG، WAV یا WebM با حداکثر ۲۰ مگابایت مجاز هستند."
                    }
                });
            }

            buffer.Position = 0;
            var objectKey = ObjectStorageKeys.Create(
                ObjectStorageKeys.Modules.Voices,
                "files",
                file.FileName);

            var uploaded = await _objectStorage.UploadAsync(
                buffer,
                objectKey,
                file.ContentType,
                cancellationToken);

            _logger.LogInformation("Admin action: uploaded voice audio {ObjectKey}", uploaded.ObjectKey);
            return Ok(new { url = uploaded.PublicUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice audio upload failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = new { message = "آپلود ناموفق بود." } });
        }
    }
}
