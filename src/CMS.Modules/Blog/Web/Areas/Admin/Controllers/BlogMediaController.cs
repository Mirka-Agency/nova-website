using CMS.Application.Common.Features;
using CMS.Application.Storage;
using CMS.Modules.Blog.Web.Upload;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Blog.Web.Areas.Admin.Controllers;

/// <summary>Media upload endpoints for the Blog module (CKEditor images + cover videos).</summary>
[Area("Admin")]
[Authorize(Policy = "ViewBlog")]
[Route("admin/blogmedia")]
public class BlogMediaController : Controller
{
    private readonly IObjectStorage _objectStorage;
    private readonly IFeatureManager _features;
    private readonly ILogger<BlogMediaController> _logger;

    public BlogMediaController(
        IObjectStorage objectStorage,
        IFeatureManager features,
        ILogger<BlogMediaController> logger)
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
        if (!await _features.IsEnabledAsync(FeatureNames.Blog))
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
                ObjectStorageKeys.Modules.Blog,
                "content",
                upload.FileName);

            var uploaded = await _objectStorage.UploadAsync(
                buffer,
                objectKey,
                upload.ContentType,
                cancellationToken);

            _logger.LogInformation("Admin action: uploaded blog content image {ObjectKey}", uploaded.ObjectKey);
            return Ok(new { url = uploaded.PublicUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blog content image upload failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = new { message = "آپلود ناموفق بود." } });
        }
    }

    [HttpPost("UploadVideo")]
    [Authorize(Policy = "ManageBlog")]
    [RequestSizeLimit(VideoUploadRules.MaxBytes + 2_097_152)]
    [RequestFormLimits(MultipartBodyLengthLimit = VideoUploadRules.MaxBytes + 2_097_152)]
    public async Task<IActionResult> UploadVideo(IFormFile? file, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Blog))
            return NotFound();

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "فایلی آپلود نشده است." });

        if (!VideoUploadRules.IsWithinSizeLimit(file.Length))
            return BadRequest(new { message = "حجم ویدیو باید حداکثر ۲۰۰ مگابایت باشد." });

        try
        {
            var contentType = NormalizeVideoContentType(file.ContentType, file.FileName);
            return await PersistVideoAsync(file, file.FileName, contentType, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("blog cover video upload canceled by client");
            return BadRequest(new { message = "آپلود قطع شد." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "blog cover video upload failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "blog cover video upload failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "آپلود ویدیو ناموفق بود." });
        }
    }

    public sealed record InitVideoUploadRequest(
        string? FileName,
        string? ContentType,
        long TotalBytes);

    private static string? NormalizeVideoContentType(string? contentType, string? fileName)
    {
        if (VideoUploadRules.IsAllowedContentType(contentType))
            return contentType!.Trim();

        var ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        return ext switch
        {
            ".mp4" or ".m4v" => "video/mp4",
            ".webm" => "video/webm",
            ".ogg" or ".ogv" => "video/ogg",
            ".mov" or ".qt" => "video/quicktime",
            _ => contentType
        };
    }

    [HttpPost("InitVideoUpload")]
    [Authorize(Policy = "ManageBlog")]
    public async Task<IActionResult> InitVideoUpload(
        [FromBody] InitVideoUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Blog))
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.FileName))
            return BadRequest(new { message = "نام فایل معتبر نیست." });

        var contentType = NormalizeVideoContentType(request.ContentType, request.FileName);
        if (!VideoUploadRules.IsAllowedContentType(contentType))
            return BadRequest(new { message = "فقط ویدیوهای MP4، WebM، OGG یا QuickTime معتبر مجاز هستند." });

        if (!VideoUploadRules.IsWithinSizeLimit(request.TotalBytes))
            return BadRequest(new { message = "حجم ویدیو باید حداکثر ۲۰۰ مگابایت باشد." });

        if (request.TotalBytes <= VideoUploadRules.ChunkThresholdBytes)
            return BadRequest(new { message = "برای فایل‌های کوچک‌تر از ۵ مگابایت از آپلود عادی استفاده کنید." });

        cancellationToken.ThrowIfCancellationRequested();

        var totalChunks = VideoUploadRules.GetChunkCount(request.TotalBytes);
        var session = BlogVideoChunkUploadStore.Create(
            request.FileName.Trim(),
            contentType!,
            request.TotalBytes,
            totalChunks);

        return Ok(new
        {
            uploadId = session.UploadId,
            chunkSize = VideoUploadRules.ChunkSizeBytes,
            totalChunks = session.TotalChunks
        });
    }

    [HttpPost("UploadVideoChunk")]
    [Authorize(Policy = "ManageBlog")]
    [RequestSizeLimit(VideoUploadRules.ChunkSizeBytes + 2_097_152)]
    [RequestFormLimits(MultipartBodyLengthLimit = VideoUploadRules.ChunkSizeBytes + 2_097_152)]
    public async Task<IActionResult> UploadVideoChunk(
        [FromForm] string? uploadId,
        [FromForm] int chunkIndex,
        IFormFile? chunk,
        CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Blog))
            return NotFound();

        var session = BlogVideoChunkUploadStore.TryGet(uploadId ?? string.Empty);
        if (session is null)
            return BadRequest(new { message = "نشست آپلود یافت نشد. دوباره تلاش کنید." });

        if (chunkIndex < 0 || chunkIndex >= session.TotalChunks)
            return BadRequest(new { message = "شماره تکه نامعتبر است." });

        if (chunk is null || chunk.Length == 0)
            return BadRequest(new { message = "تکه خالی است." });

        if (chunk.Length > VideoUploadRules.ChunkSizeBytes + 1024)
            return BadRequest(new { message = "حجم تکه بیش از حد مجاز است." });

        try
        {
            var path = BlogVideoChunkUploadStore.GetChunkPath(session.UploadId, chunkIndex);
            await using (var fs = new FileStream(
                             path,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             1024 * 64,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await chunk.CopyToAsync(fs, cancellationToken);
            }

            session.ReceivedChunks.Add(chunkIndex);
            BlogVideoChunkUploadStore.Save(session);

            return Ok(new
            {
                uploadId = session.UploadId,
                chunkIndex,
                received = session.ReceivedChunks.Count,
                totalChunks = session.TotalChunks
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return BadRequest(new { message = "آپلود قطع شد." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "blog video chunk upload failed for {UploadId} chunk {ChunkIndex}", uploadId, chunkIndex);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "آپلود تکه ناموفق بود." });
        }
    }

    [HttpPost("CompleteVideoUpload")]
    [Authorize(Policy = "ManageBlog")]
    public async Task<IActionResult> CompleteVideoUpload(
        [FromBody] CompleteVideoUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Blog))
            return NotFound();

        var session = BlogVideoChunkUploadStore.TryGet(request.UploadId ?? string.Empty);
        if (session is null)
            return BadRequest(new { message = "نشست آپلود یافت نشد. دوباره تلاش کنید." });

        if (session.ReceivedChunks.Count != session.TotalChunks
            || Enumerable.Range(0, session.TotalChunks).Any(i => !session.ReceivedChunks.Contains(i)))
        {
            return BadRequest(new { message = "همه تکه‌های ویدیو دریافت نشده‌اند." });
        }

        var assembledPath = Path.Combine(Path.GetTempPath(), $"cms-blog-video-assembled-{session.UploadId}");
        try
        {
            await using (var output = new FileStream(
                             assembledPath,
                             FileMode.CreateNew,
                             FileAccess.ReadWrite,
                             FileShare.None,
                             1024 * 64,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                for (var i = 0; i < session.TotalChunks; i++)
                {
                    var partPath = BlogVideoChunkUploadStore.GetChunkPath(session.UploadId, i);
                    if (!System.IO.File.Exists(partPath))
                        return BadRequest(new { message = $"تکه {i + 1} یافت نشد." });

                    await using var part = new FileStream(
                        partPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        1024 * 64,
                        FileOptions.Asynchronous | FileOptions.SequentialScan);
                    await part.CopyToAsync(output, cancellationToken);
                }

                if (output.Length != session.TotalBytes)
                {
                    return BadRequest(new
                    {
                        message = "حجم فایل مونتاژشده با اندازه اعلام‌شده هم‌خوانی ندارد."
                    });
                }

                output.Position = 0;
                if (!VideoUploadRules.Validate(output, session.ContentType, output.Length))
                {
                    return BadRequest(new
                    {
                        message = "فقط ویدیوهای MP4، WebM، OGG یا QuickTime معتبر مجاز هستند."
                    });
                }

                output.Position = 0;
                var objectKey = ObjectStorageKeys.Create(
                    ObjectStorageKeys.Modules.Blog,
                    "files",
                    session.FileName);

                var uploaded = await _objectStorage.UploadAsync(
                    output,
                    objectKey,
                    session.ContentType,
                    cancellationToken);

                _logger.LogInformation(
                    "Admin action: uploaded chunked blog cover video {ObjectKey} ({Bytes} bytes)",
                    uploaded.ObjectKey,
                    session.TotalBytes);

                return Ok(new { url = uploaded.PublicUrl });
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return BadRequest(new { message = "آپلود قطع شد." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "chunked blog video upload finalize failed for {UploadId}", session.UploadId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "chunked blog video upload finalize failed for {UploadId}", session.UploadId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "اتمام آپلود ویدیو ناموفق بود." });
        }
        finally
        {
            BlogVideoChunkUploadStore.Delete(session.UploadId);
            try
            {
                if (System.IO.File.Exists(assembledPath))
                    System.IO.File.Delete(assembledPath);
            }
            catch
            {
                // ignore
            }
        }
    }

    public sealed record CompleteVideoUploadRequest(string? UploadId);

    [HttpPost("AbortVideoUpload")]
    [Authorize(Policy = "ManageBlog")]
    public async Task<IActionResult> AbortVideoUpload(
        [FromBody] CompleteVideoUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Blog))
            return NotFound();

        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrWhiteSpace(request.UploadId))
            BlogVideoChunkUploadStore.Delete(request.UploadId);

        return Ok(new { aborted = true });
    }

    private async Task<IActionResult> PersistVideoAsync(
        IFormFile file,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"cms-blog-video-{Guid.NewGuid():N}");
        await using var temp = new FileStream(
            tempPath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            1024 * 64,
            FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);

        await file.CopyToAsync(temp, cancellationToken);
        temp.Position = 0;

        if (!VideoUploadRules.Validate(temp, contentType, temp.Length))
        {
            return BadRequest(new
            {
                message = "فقط ویدیوهای MP4، WebM، OGG یا QuickTime معتبر مجاز هستند."
            });
        }

        temp.Position = 0;
        var objectKey = ObjectStorageKeys.Create(
            ObjectStorageKeys.Modules.Blog,
            "files",
            fileName);

        var uploaded = await _objectStorage.UploadAsync(
            temp,
            objectKey,
            contentType!,
            cancellationToken);

        _logger.LogInformation("Admin action: uploaded blog cover video {ObjectKey}", uploaded.ObjectKey);
        return Ok(new { url = uploaded.PublicUrl });
    }
}
