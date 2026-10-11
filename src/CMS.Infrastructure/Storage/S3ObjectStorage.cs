using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using CMS.Application.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Storage;

public sealed class S3ObjectStorage : IObjectStorage, IDisposable
{
    /// <summary>
    /// Above this size, use multipart TransferUtility instead of a single PutObject.
    /// Large single PutObject calls often time out against S3-compatible providers (Arvan).
    /// </summary>
    private const long MultipartThresholdBytes = 8 * 1024 * 1024;

    private readonly IAmazonS3 _client;
    private readonly S3ObjectStorageOptions _options;
    private readonly ILogger<S3ObjectStorage> _logger;

    public S3ObjectStorage(
        IOptions<S3ObjectStorageOptions> options,
        ILogger<S3ObjectStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = CreateClient(_options);
    }

    public async Task<ObjectStorageUploadResult> UploadAsync(
        Stream content,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        EnsureConfigured();

        var key = objectKey.Trim().TrimStart('/');
        var cacheControl = ObjectStorageCachePolicy.ResolveCacheControl(key);

        // Prefer the caller's seekable stream (e.g. video temp file). Only buffer when needed.
        Stream uploadStream = content;
        MemoryStream? ownedBuffer = null;
        long length;
        try
        {
            if (!content.CanSeek || content.Length <= 0)
            {
                ownedBuffer = new MemoryStream();
                if (content.CanSeek)
                    content.Position = 0;
                await content.CopyToAsync(ownedBuffer, cancellationToken);
                ownedBuffer.Position = 0;
                uploadStream = ownedBuffer;
            }
            else if (content.Position != 0)
            {
                content.Position = 0;
            }

            length = uploadStream.Length;
            if (length <= 0)
                throw new InvalidOperationException("فایل خالی است و قابل آپلود نیست.");

            if (length >= MultipartThresholdBytes)
            {
                await UploadMultipartAsync(uploadStream, key, contentType, cacheControl, cancellationToken);
            }
            else
            {
                await UploadPutObjectAsync(uploadStream, key, contentType, cacheControl, cancellationToken);
            }
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "S3 upload canceled by client for {ObjectKey} in bucket {Bucket}", key, _options.BucketName);
            throw new InvalidOperationException("آپلود توسط کاربر یا مرورگر قطع شد.", ex);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "S3 upload timed out for {ObjectKey} in bucket {Bucket}", key, _options.BucketName);
            throw new InvalidOperationException(
                "آپلود به فضای ذخیره‌سازی زمانش تمام شد. اتصال شبکه یا محدودیت provider را بررسی کنید.",
                ex);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 upload failed for {ObjectKey} in bucket {Bucket}: {ErrorCode}",
                key, _options.BucketName, ex.ErrorCode);
            throw new InvalidOperationException(
                $"آپلود S3 ناموفق بود ({ex.ErrorCode}): {ex.Message}",
                ex);
        }
        finally
        {
            if (ownedBuffer is not null)
                await ownedBuffer.DisposeAsync();
        }

        var publicUrl = GetPublicUrl(key);
        _logger.LogInformation("Uploaded object {ObjectKey} ({Length} bytes) to bucket {Bucket}",
            key, length, _options.BucketName);
        return new ObjectStorageUploadResult(key, publicUrl);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        EnsureConfigured();

        var key = objectKey.Trim().TrimStart('/');
        try
        {
            await _client.DeleteObjectAsync(_options.BucketName, key, cancellationToken);
            _logger.LogInformation("Deleted object {ObjectKey} from bucket {Bucket}", key, _options.BucketName);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Idempotent delete
        }
    }

    public string GetPublicUrl(string objectKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        EnsureConfigured();

        var key = objectKey.Trim().TrimStart('/');
        var encodedKey = string.Join('/', key.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));

        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return $"{_options.PublicBaseUrl.TrimEnd('/')}/{encodedKey}";

        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            var endpoint = _options.ServiceUrl.TrimEnd('/');
            if (ShouldForcePathStyle(_options))
                return $"{endpoint}/{_options.BucketName}/{encodedKey}";

            var host = new Uri(endpoint);
            return $"{host.Scheme}://{_options.BucketName}.{host.Authority}/{encodedKey}";
        }

        return $"https://{_options.BucketName}.s3.{_options.Region}.amazonaws.com/{encodedKey}";
    }

    public async Task<ObjectStorageCacheBackfillResult> BackfillCacheHeadersAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var examined = 0;
        var updated = 0;
        var skipped = 0;
        var failed = 0;
        string? continuationToken = null;

        do
        {
            var list = await _client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _options.BucketName,
                ContinuationToken = continuationToken,
                MaxKeys = 200
            }, cancellationToken);

            foreach (var obj in list.S3Objects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                examined++;

                if (!ObjectStorageCachePolicy.IsPublicImmutableCandidate(obj.Key))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    if (await TryApplyPublicCacheHeaderAsync(obj.Key, cancellationToken))
                        updated++;
                    else
                        skipped++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogWarning(ex, "Failed to backfill Cache-Control for {ObjectKey}", obj.Key);
                }
            }

            continuationToken = list.IsTruncated == true ? list.NextContinuationToken : null;
        } while (continuationToken is not null);

        _logger.LogInformation(
            "S3 Cache-Control backfill finished for bucket {Bucket}: examined={Examined}, updated={Updated}, skipped={Skipped}, failed={Failed}",
            _options.BucketName, examined, updated, skipped, failed);

        return new ObjectStorageCacheBackfillResult(examined, updated, skipped, failed);
    }

    public void Dispose() => _client.Dispose();

    /// <summary>
    /// Returns true when metadata was written; false when already correct / not a candidate.
    /// </summary>
    private async Task<bool> TryApplyPublicCacheHeaderAsync(string key, CancellationToken cancellationToken)
    {
        GetObjectMetadataResponse metadata;
        try
        {
            metadata = await _client.GetObjectMetadataAsync(_options.BucketName, key, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        if (ObjectStorageCachePolicy.HasDesiredCacheControl(key, metadata.Headers.CacheControl))
            return false;

        var desired = ObjectStorageCachePolicy.ResolveCacheControl(key);
        if (desired is null)
            return false;

        var copy = new CopyObjectRequest
        {
            SourceBucket = _options.BucketName,
            SourceKey = key,
            DestinationBucket = _options.BucketName,
            DestinationKey = key,
            MetadataDirective = S3MetadataDirective.REPLACE,
            ContentType = string.IsNullOrWhiteSpace(metadata.Headers.ContentType)
                ? "application/octet-stream"
                : metadata.Headers.ContentType
        };
        copy.Headers.CacheControl = desired;

        if (!string.IsNullOrWhiteSpace(metadata.Headers.ContentDisposition))
            copy.Headers.ContentDisposition = metadata.Headers.ContentDisposition;
        if (!string.IsNullOrWhiteSpace(metadata.Headers.ContentEncoding))
            copy.Headers.ContentEncoding = metadata.Headers.ContentEncoding;

        foreach (var metaKey in metadata.Metadata.Keys)
            copy.Metadata[metaKey] = metadata.Metadata[metaKey];

        if (_options.UsePublicReadAcl)
            copy.CannedACL = S3CannedACL.PublicRead;

        try
        {
            await _client.CopyObjectAsync(copy, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (_options.UsePublicReadAcl
            && ex.StatusCode is System.Net.HttpStatusCode.BadRequest
                or System.Net.HttpStatusCode.Forbidden
                or System.Net.HttpStatusCode.NotImplemented)
        {
            _logger.LogWarning(ex, "S3 ACL public-read rejected while backfilling {ObjectKey}; retrying without ACL", key);
            copy.CannedACL = null;
            await _client.CopyObjectAsync(copy, cancellationToken);
        }

        return true;
    }

    private async Task UploadPutObjectAsync(
        Stream content,
        string key,
        string contentType,
        string? cacheControl,
        CancellationToken cancellationToken)
    {
        content.Position = 0;
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            // Arvan / many S3-compatible APIs reject aws-chunked transfer encoding.
            UseChunkEncoding = false,
            // Avoid buffering the whole payload to compute SigV4 payload hash (slow/timeouts on large videos).
            DisablePayloadSigning = true
        };
        request.Headers.ContentLength = content.Length;
        ApplyCacheControl(request.Headers, cacheControl);

        if (_options.UsePublicReadAcl)
            request.CannedACL = S3CannedACL.PublicRead;

        try
        {
            await _client.PutObjectAsync(request, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (_options.UsePublicReadAcl
            && ex.StatusCode is System.Net.HttpStatusCode.BadRequest
                or System.Net.HttpStatusCode.Forbidden
                or System.Net.HttpStatusCode.NotImplemented)
        {
            _logger.LogWarning(ex, "S3 ACL public-read rejected for {ObjectKey}; retrying without ACL", key);
            content.Position = 0;
            request.CannedACL = null;
            await _client.PutObjectAsync(request, cancellationToken);
        }
    }

    private async Task UploadMultipartAsync(
        Stream content,
        string key,
        string contentType,
        string? cacheControl,
        CancellationToken cancellationToken)
    {
        content.Position = 0;
        using var transfer = new TransferUtility(_client, new TransferUtilityConfig
        {
            // Single part at a time is more reliable on constrained / S3-compatible links.
            ConcurrentServiceRequests = 1,
            MinSizeBeforePartUpload = MultipartThresholdBytes
        });

        var request = new TransferUtilityUploadRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            PartSize = MultipartThresholdBytes,
            DisablePayloadSigning = true
        };
        ApplyCacheControl(request.Headers, cacheControl);

        if (_options.UsePublicReadAcl)
            request.CannedACL = S3CannedACL.PublicRead;

        try
        {
            await transfer.UploadAsync(request, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (_options.UsePublicReadAcl
            && ex.StatusCode is System.Net.HttpStatusCode.BadRequest
                or System.Net.HttpStatusCode.Forbidden
                or System.Net.HttpStatusCode.NotImplemented)
        {
            _logger.LogWarning(ex, "S3 ACL public-read rejected for multipart {ObjectKey}; retrying without ACL", key);
            content.Position = 0;
            request.CannedACL = null;
            await transfer.UploadAsync(request, cancellationToken);
        }
    }

    private static void ApplyCacheControl(HeadersCollection headers, string? cacheControl)
    {
        if (!string.IsNullOrWhiteSpace(cacheControl))
            headers.CacheControl = cacheControl;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.AccessKey)
            || string.IsNullOrWhiteSpace(_options.SecretKey)
            || string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException(
                "Storage:S3 is not configured. Set AccessKey, SecretKey, and BucketName " +
                "(see .env.example: Storage__S3__*).");
        }
    }

    private static IAmazonS3 CreateClient(S3ObjectStorageOptions options)
    {
        var accessKey = string.IsNullOrWhiteSpace(options.AccessKey) ? "not-configured" : options.AccessKey;
        var secretKey = string.IsNullOrWhiteSpace(options.SecretKey) ? "not-configured" : options.SecretKey;
        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var config = new AmazonS3Config
        {
            ForcePathStyle = ShouldForcePathStyle(options),
            // Videos can be up to 200 MB; single/part uploads over constrained links need headroom.
            Timeout = TimeSpan.FromMinutes(30),
            MaxErrorRetry = 2,
            UseHttp = false,
            // Trailing checksums / aws-chunked break many S3-compatible providers (Arvan).
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
        };

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl.TrimEnd('/');
            config.AuthenticationRegion = string.IsNullOrWhiteSpace(options.Region)
                ? "us-east-1"
                : options.Region;
        }
        else
        {
            var region = string.IsNullOrWhiteSpace(options.Region) ? "us-east-1" : options.Region;
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
        }

        return new AmazonS3Client(credentials, config);
    }

    /// <summary>
    /// Path-style is required for most S3-compatible providers (Arvan, MinIO, Liara).
    /// When ServiceUrl is set and ForcePathStyle was left at default false, enable it automatically.
    /// </summary>
    private static bool ShouldForcePathStyle(S3ObjectStorageOptions options) =>
        options.ForcePathStyle
        || !string.IsNullOrWhiteSpace(options.ServiceUrl);
}
