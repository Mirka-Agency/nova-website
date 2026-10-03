namespace CMS.Modules.Media.Application.Imaging;

public sealed class OptimizedMediaFile : IDisposable
{
    public OptimizedMediaFile(
        string sizeName,
        Stream content,
        string contentType,
        string fileExtension,
        int width,
        int height,
        long sizeBytes)
    {
        SizeName = sizeName;
        Content = content;
        ContentType = contentType;
        FileExtension = fileExtension;
        Width = width;
        Height = height;
        SizeBytes = sizeBytes;
    }

    public string SizeName { get; }
    public Stream Content { get; }
    public string ContentType { get; }
    public string FileExtension { get; }
    public int Width { get; }
    public int Height { get; }
    public long SizeBytes { get; }

    public void Dispose() => Content.Dispose();
}

public sealed class MediaImageOptimizationResult : IDisposable
{
    public MediaImageOptimizationResult(
        int sourceWidth,
        int sourceHeight,
        bool usedOriginalAsDisplay,
        OptimizedMediaFile? display,
        OptimizedMediaFile? thumbnail,
        IReadOnlyList<OptimizedMediaFile> additionalSizes)
    {
        SourceWidth = sourceWidth;
        SourceHeight = sourceHeight;
        UsedOriginalAsDisplay = usedOriginalAsDisplay;
        Display = display;
        Thumbnail = thumbnail;
        AdditionalSizes = additionalSizes;
    }

    public int SourceWidth { get; }
    public int SourceHeight { get; }
    public bool UsedOriginalAsDisplay { get; }
    public OptimizedMediaFile? Display { get; }
    public OptimizedMediaFile? Thumbnail { get; }
    public IReadOnlyList<OptimizedMediaFile> AdditionalSizes { get; }

    public static MediaImageOptimizationResult FromOriginalOnly(int width, int height) =>
        new(width, height, usedOriginalAsDisplay: true, display: null, thumbnail: null, additionalSizes: []);

    public void Dispose()
    {
        Display?.Dispose();
        Thumbnail?.Dispose();
        foreach (var size in AdditionalSizes)
            size.Dispose();
    }
}

/// <summary>
/// Optional per-upload resize / compression overrides.
/// Null properties keep the built-in defaults (max edge 2560, quality 82).
/// </summary>
public sealed class MediaOptimizeOptions
{
    public static MediaOptimizeOptions Default { get; } = new();

    public const int MinQuality = 40;
    public const int MaxQuality = 100;
    public const int MinDimension = 16;
    public const int MaxDimension = 8192;

    /// <summary>Max width/height of the display WebP (longest edge). Null = default.</summary>
    public int? MaxDisplayDimension { get; init; }

    /// <summary>WebP quality 40–100. Null = default (82).</summary>
    public int? Quality { get; init; }

    public static MediaOptimizeOptions? FromOptional(int? maxDisplayDimension, int? quality)
    {
        if (maxDisplayDimension is null && quality is null)
            return null;

        return new MediaOptimizeOptions
        {
            MaxDisplayDimension = maxDisplayDimension,
            Quality = quality
        };
    }
}

public interface IMediaImageOptimizer
{
    Task<MediaImageOptimizationResult> OptimizeAsync(
        Stream original,
        string contentType,
        MediaOptimizeOptions? options = null,
        CancellationToken cancellationToken = default);
}
