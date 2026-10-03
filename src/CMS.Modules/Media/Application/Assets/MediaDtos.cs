using CMS.Modules.Media.Application.Imaging;

namespace CMS.Modules.Media.Application.Assets;

public sealed record MediaAssetListItemDto(
    Guid Id,
    string FileName,
    string? Title,
    string? AltText,
    string ContentType,
    long SizeBytes,
    string PublicUrl,
    string? ThumbnailPublicUrl,
    DateTime CreatedAtUtc);

public sealed record MediaImageVariantDto(
    string Name,
    string PublicUrl,
    int Width,
    int Height,
    long SizeBytes,
    string ContentType);

public sealed record MediaAssetDetailDto(
    Guid Id,
    string FileName,
    string? Title,
    string? AltText,
    string? Caption,
    string? Description,
    string ContentType,
    long SizeBytes,
    string ObjectKey,
    string PublicUrl,
    int? Width,
    int? Height,
    string? ThumbnailPublicUrl,
    string? OriginalFileName,
    string? OriginalContentType,
    long? OriginalSizeBytes,
    string? OriginalPublicUrl,
    bool HasSeparateOriginal,
    IReadOnlyList<MediaImageVariantDto> Variants,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record UploadMediaCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Title = null,
    string? AltText = null,
    string? Caption = null,
    string? Description = null,
    MediaOptimizeOptions? Optimize = null);

public sealed record UpdateMediaMetadataCommand(
    string? Title,
    string? AltText,
    string? Caption,
    string? Description);

public sealed record ReplaceMediaFileCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes,
    MediaOptimizeOptions? Optimize = null);
