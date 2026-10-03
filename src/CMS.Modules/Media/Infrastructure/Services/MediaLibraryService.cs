using CMS.Application.Common.Paging;
using CMS.Application.Storage;
using CMS.Domain.Exceptions;
using CMS.Modules.Media.Application.Assets;
using CMS.Modules.Media.Application.Imaging;
using CMS.Modules.Media.Application.Interfaces;
using CMS.Modules.Media.Domain.Entities;
using CMS.Modules.Media.Domain.ValueObjects;
using CMS.Modules.Media.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Media.Infrastructure.Services;

public sealed class MediaLibraryService : IMediaLibraryService
{
    private readonly MediaDbContext _db;
    private readonly IObjectStorage _storage;
    private readonly IMediaImageOptimizer _optimizer;
    private readonly IValidator<UploadMediaCommand> _uploadValidator;
    private readonly IValidator<ReplaceMediaFileCommand> _replaceValidator;
    private readonly ILogger<MediaLibraryService> _logger;

    public MediaLibraryService(
        MediaDbContext db,
        IObjectStorage storage,
        IMediaImageOptimizer optimizer,
        IValidator<UploadMediaCommand> uploadValidator,
        IValidator<ReplaceMediaFileCommand> replaceValidator,
        ILogger<MediaLibraryService> logger)
    {
        _db = db;
        _storage = storage;
        _optimizer = optimizer;
        _uploadValidator = uploadValidator;
        _replaceValidator = replaceValidator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MediaAssetListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var page = await ListPagedAsync(new PagedRequest { Page = 1, PageSize = PagedRequest.MaxPageSize }, cancellationToken);
        return page.Items;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.Assets.AsNoTracking().CountAsync(cancellationToken);

    public async Task<PagedResult<MediaAssetListItemDto>> ListPagedAsync(
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Assets.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(a =>
                a.FileName.Contains(term) ||
                a.ContentType.Contains(term) ||
                (a.Title != null && a.Title.Contains(term)) ||
                (a.AltText != null && a.AltText.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(a => new
            {
                a.Id,
                a.FileName,
                a.Title,
                a.AltText,
                a.ContentType,
                a.SizeBytes,
                a.ObjectKey,
                a.ThumbnailObjectKey,
                a.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(a => new MediaAssetListItemDto(
                a.Id,
                a.FileName,
                a.Title,
                a.AltText,
                a.ContentType,
                a.SizeBytes,
                ResolvePublicUrl(a.ObjectKey),
                ResolveOptionalPublicUrl(a.ThumbnailObjectKey),
                a.CreatedAtUtc))
            .ToList();

        return new PagedResult<MediaAssetListItemDto>(items, total, request.NormalizedPage, request.NormalizedPageSize);
    }

    public async Task<MediaAssetDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await _db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        return asset is null ? null : MapDetail(asset);
    }

    public async Task<MediaAssetDetailDto?> FindByPublicUrlAsync(
        string publicUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicUrl))
            return null;

        var candidateKeys = ExtractObjectKeyCandidates(publicUrl).ToList();
        if (candidateKeys.Count == 0)
            return null;

        var assets = await _db.Assets.AsNoTracking()
            .Where(a =>
                candidateKeys.Contains(a.ObjectKey) ||
                (a.ThumbnailObjectKey != null && candidateKeys.Contains(a.ThumbnailObjectKey)) ||
                (a.OriginalObjectKey != null && candidateKeys.Contains(a.OriginalObjectKey)))
            .ToListAsync(cancellationToken);

        if (assets.Count == 0)
            return null;

        if (assets.Count == 1)
            return MapDetail(assets[0]);

        var normalized = NormalizePublicUrl(publicUrl);
        foreach (var asset in assets)
        {
            var detail = MapDetail(asset);
            if (UrlsEqual(normalized, detail.PublicUrl)
                || UrlsEqual(normalized, detail.ThumbnailPublicUrl)
                || UrlsEqual(normalized, detail.OriginalPublicUrl))
            {
                return detail;
            }
        }

        return MapDetail(assets[0]);
    }

    public async Task<MediaAssetDetailDto> UploadAsync(UploadMediaCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_uploadValidator, command, cancellationToken);
        ValidateImageStream(command.Content, command.ContentType, command.SizeBytes);

        await using var originalBuffer = await BufferAsync(command.Content, cancellationToken);
        var stored = await StoreOriginalAndDerivativesAsync(
            originalBuffer,
            command.FileName,
            command.ContentType,
            command.SizeBytes,
            cancellationToken,
            storeOriginal: true,
            command.Optimize);

        var asset = MediaAsset.Create(
            stored.Display,
            stored.Original,
            command.Title,
            command.AltText,
            command.Caption,
            command.Description,
            stored.Thumbnail,
            stored.Variants);

        _db.Assets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);
        return MapDetail(asset);
    }

    public async Task UpdateMetadataAsync(Guid id, UpdateMediaMetadataCommand command, CancellationToken cancellationToken = default)
    {
        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(MediaAsset), id);

        asset.UpdateMetadata(command.Title, command.AltText, command.Caption, command.Description);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MediaAssetDetailDto> ReplaceFileAsync(
        Guid id,
        ReplaceMediaFileCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_replaceValidator, command, cancellationToken);

        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(MediaAsset), id);

        ValidateImageStream(command.Content, command.ContentType, command.SizeBytes);

        await using var buffer = await BufferAsync(command.Content, cancellationToken);
        var stored = await StoreOriginalAndDerivativesAsync(
            buffer,
            command.FileName,
            command.ContentType,
            command.SizeBytes,
            cancellationToken,
            storeOriginal: false,
            command.Optimize);

        var staleKeys = asset.ReplaceDisplay(stored.Display, stored.Thumbnail, stored.Variants);
        await _db.SaveChangesAsync(cancellationToken);
        await DeleteBestEffortAsync(staleKeys, cancellationToken);
        return MapDetail(asset);
    }

    public async Task<MediaAssetDetailDto> RestoreOriginalAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(MediaAsset), id);

        var staleKeys = asset.RestoreOriginal();
        await _db.SaveChangesAsync(cancellationToken);
        await DeleteBestEffortAsync(staleKeys, cancellationToken);
        return MapDetail(asset);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await _db.Assets.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(MediaAsset), id);

        var keys = asset.GetAllStorageKeys();
        _db.Assets.Remove(asset);
        await _db.SaveChangesAsync(cancellationToken);
        await DeleteBestEffortAsync(keys, cancellationToken);
    }

    private async Task<StoredMediaFiles> StoreOriginalAndDerivativesAsync(
        MemoryStream originalBuffer,
        string fileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken,
        bool storeOriginal = true,
        MediaOptimizeOptions? optimize = null)
    {
        originalBuffer.Position = 0;
        ObjectStorageUploadResult? originalUpload = null;
        if (storeOriginal)
        {
            var originalKey = ObjectStorageKeys.Create(
                ObjectStorageKeys.Modules.Media,
                "library",
                fileName);
            originalUpload = await _storage.UploadAsync(originalBuffer, originalKey, contentType, cancellationToken);
        }

        originalBuffer.Position = 0;
        MediaImageOptimizationResult? optimized = null;
        try
        {
            optimized = await _optimizer.OptimizeAsync(
                originalBuffer, contentType, optimize ?? MediaOptimizeOptions.Default, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Image optimization failed for {FileName}; keeping original as display", fileName);
        }

        try
        {
            var originalRef = originalUpload is null
                ? null
                : new MediaFileRef(
                    fileName,
                    contentType,
                    sizeBytes,
                    originalUpload.ObjectKey,
                    optimized?.SourceWidth,
                    optimized?.SourceHeight);

            var thumbnail = optimized?.Thumbnail is null
                ? null
                : await UploadDerivativeAsync(optimized.Thumbnail, fileName, cancellationToken);

            var extra = new List<MediaImageVariant>();
            if (optimized is not null)
            {
                foreach (var size in optimized.AdditionalSizes)
                {
                    var uploaded = await UploadDerivativeAsync(size, fileName, cancellationToken);
                    extra.Add(new MediaImageVariant(
                        size.SizeName,
                        uploaded.ObjectKey,
                        size.Width,
                        size.Height,
                        size.SizeBytes,
                        size.ContentType));
                }
            }

            MediaFileRef display;
            if (optimized?.Display is { } displayFile)
            {
                display = await UploadDerivativeAsync(displayFile, fileName, cancellationToken);
            }
            else if (originalRef is not null)
            {
                display = originalRef;
            }
            else
            {
                var fallbackKey = ObjectStorageKeys.Create(
                    ObjectStorageKeys.Modules.Media,
                    "library",
                    fileName);
                originalBuffer.Position = 0;
                var fallback = await _storage.UploadAsync(originalBuffer, fallbackKey, contentType, cancellationToken);
                display = new MediaFileRef(
                    fileName,
                    contentType,
                    sizeBytes,
                    fallback.ObjectKey,
                    optimized?.SourceWidth,
                    optimized?.SourceHeight);
            }

            return new StoredMediaFiles(
                originalRef ?? display,
                display,
                thumbnail,
                extra);
        }
        finally
        {
            optimized?.Dispose();
        }
    }

    private async Task<MediaFileRef> UploadDerivativeAsync(
        OptimizedMediaFile file,
        string originalFileName,
        CancellationToken cancellationToken)
    {
        var name = Path.GetFileNameWithoutExtension(originalFileName);
        var derivedName = $"{name}-{file.SizeName}{file.FileExtension}";
        var key = ObjectStorageKeys.Create(
            ObjectStorageKeys.Modules.Media,
            "library",
            derivedName);

        file.Content.Position = 0;
        var uploaded = await _storage.UploadAsync(file.Content, key, file.ContentType, cancellationToken);
        return new MediaFileRef(
            derivedName,
            file.ContentType,
            file.SizeBytes,
            uploaded.ObjectKey,
            file.Width,
            file.Height);
    }

    private async Task DeleteBestEffortAsync(IEnumerable<string> keys, CancellationToken cancellationToken)
    {
        foreach (var key in keys.Distinct(StringComparer.Ordinal))
        {
            try
            {
                await _storage.DeleteAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete media object {ObjectKey}", key);
            }
        }
    }

    private static async Task<MemoryStream> BufferAsync(Stream content, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        if (content.CanSeek)
            content.Position = 0;
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return buffer;
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T command, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private static void ValidateImageStream(Stream content, string contentType, long sizeBytes)
    {
        if (!content.CanSeek
            || !ImageUploadRules.Validate(content, contentType, sizeBytes))
        {
            throw new DomainValidationException(new Dictionary<string, string[]>
            {
                [nameof(contentType)] = ["فقط تصاویر JPEG، PNG، WebP یا GIF مجاز هستند."]
            });
        }
    }

    private MediaAssetDetailDto MapDetail(MediaAsset asset) =>
        new(
            asset.Id,
            asset.FileName,
            asset.Title,
            asset.AltText,
            asset.Caption,
            asset.Description,
            asset.ContentType,
            asset.SizeBytes,
            asset.ObjectKey,
            ResolvePublicUrl(asset.ObjectKey),
            asset.Width,
            asset.Height,
            ResolveOptionalPublicUrl(asset.ThumbnailObjectKey),
            asset.OriginalFileName,
            asset.OriginalContentType,
            asset.OriginalSizeBytes,
            ResolveOptionalPublicUrl(asset.OriginalObjectKey),
            asset.HasSeparateOriginal,
            asset.GetVariants()
                .Select(v => new MediaImageVariantDto(
                    v.Name,
                    ResolvePublicUrl(v.ObjectKey),
                    v.Width,
                    v.Height,
                    v.SizeBytes,
                    v.ContentType))
                .ToList(),
            asset.CreatedAtUtc,
            asset.UpdatedAtUtc);

    private string ResolvePublicUrl(string objectKey) => _storage.GetPublicUrl(objectKey);

    private string? ResolveOptionalPublicUrl(string? objectKey) =>
        string.IsNullOrWhiteSpace(objectKey) ? null : _storage.GetPublicUrl(objectKey);

    private static HashSet<string> ExtractObjectKeyCandidates(string publicUrl)
    {
        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var raw = NormalizePublicUrl(publicUrl);
        if (string.IsNullOrWhiteSpace(raw))
            return results;

        string path;
        if (Uri.TryCreate(raw, UriKind.Absolute, out var absolute))
            path = Uri.UnescapeDataString(absolute.AbsolutePath);
        else
            path = Uri.UnescapeDataString(raw);

        path = path.Trim().TrimStart('/');
        if (string.IsNullOrWhiteSpace(path))
            return results;

        results.Add(path);

        var slash = path.IndexOf('/');
        if (slash > 0 && slash < path.Length - 1)
            results.Add(path[(slash + 1)..]);

        var mediaIdx = path.IndexOf("media/", StringComparison.OrdinalIgnoreCase);
        if (mediaIdx >= 0)
            results.Add(path[mediaIdx..]);

        return results;
    }

    private static string NormalizePublicUrl(string publicUrl)
    {
        var value = publicUrl.Trim();
        var cut = value.IndexOfAny(['?', '#']);
        if (cut >= 0)
            value = value[..cut];
        return value.TrimEnd('/');
    }

    private static bool UrlsEqual(string left, string? right)
    {
        if (string.IsNullOrWhiteSpace(right))
            return false;
        return string.Equals(NormalizePublicUrl(left), NormalizePublicUrl(right), StringComparison.OrdinalIgnoreCase);
    }

    private sealed record StoredMediaFiles(
        MediaFileRef Original,
        MediaFileRef Display,
        MediaFileRef? Thumbnail,
        IReadOnlyList<MediaImageVariant> Variants);
}
