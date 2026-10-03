using CMS.Modules.Media.Application.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace CMS.Modules.Media.Infrastructure.Imaging;

/// <summary>
/// Keep the original, convert display to WebP, optionally resize/compress,
/// and generate thumbnail / medium / large derivatives.
/// Animated GIFs are left as the display file so animation is preserved.
/// </summary>
public sealed class ImageSharpMediaOptimizer : IMediaImageOptimizer
{
    public const int DefaultMaxFullDimension = 2560;
    public const int LargeMax = 1024;
    public const int MediumMax = 300;
    public const int ThumbnailSize = 150;
    public const int DefaultQuality = 82;

    /// <summary>Backward-compatible alias for tests and callers.</summary>
    public const int MaxFullDimension = DefaultMaxFullDimension;

    /// <summary>Backward-compatible alias for tests and callers.</summary>
    public const int Quality = DefaultQuality;

    public async Task<MediaImageOptimizationResult> OptimizeAsync(
        Stream original,
        string contentType,
        MediaOptimizeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(original);

        if (original.CanSeek)
            original.Position = 0;

        Image image;
        try
        {
            image = await Image.LoadAsync(original, cancellationToken);
        }
        catch (UnknownImageFormatException)
        {
            return MediaImageOptimizationResult.FromOriginalOnly(0, 0);
        }
        catch (InvalidImageContentException)
        {
            return MediaImageOptimizationResult.FromOriginalOnly(0, 0);
        }

        var maxFull = ResolveMaxDisplayDimension(options?.MaxDisplayDimension);
        var quality = ResolveQuality(options?.Quality);
        var encoder = CreateEncoder(quality);

        using (image)
        {
            if (image.Frames.Count > 1)
            {
                using var firstFrame = image.Frames.CloneFrame(0);
                var thumb = await EncodeAsync(
                    firstFrame, "thumbnail", ThumbnailSize, cropSquare: true, encoder, cancellationToken);
                return new MediaImageOptimizationResult(
                    image.Width,
                    image.Height,
                    usedOriginalAsDisplay: true,
                    display: null,
                    thumbnail: thumb,
                    additionalSizes: []);
            }

            var additional = new List<OptimizedMediaFile>();
            OptimizedMediaFile? display = null;
            try
            {
                display = await EncodeAsync(
                    image, "full", maxFull, cropSquare: false, encoder, cancellationToken);

                var thumbnail = await EncodeAsync(
                    image, "thumbnail", ThumbnailSize, cropSquare: true, encoder, cancellationToken);
                additional.Add(thumbnail);

                if (image.Width > MediumMax || image.Height > MediumMax)
                    additional.Add(await EncodeAsync(
                        image, "medium", MediumMax, cropSquare: false, encoder, cancellationToken));

                if (image.Width > LargeMax || image.Height > LargeMax)
                    additional.Add(await EncodeAsync(
                        image, "large", LargeMax, cropSquare: false, encoder, cancellationToken));

                var extra = additional.Skip(1).ToList();
                return new MediaImageOptimizationResult(
                    image.Width,
                    image.Height,
                    usedOriginalAsDisplay: false,
                    display,
                    thumbnail,
                    extra);
            }
            catch
            {
                display?.Dispose();
                foreach (var file in additional)
                    file.Dispose();
                throw;
            }
        }
    }

    private static int ResolveMaxDisplayDimension(int? value)
    {
        if (value is null)
            return DefaultMaxFullDimension;

        return Math.Clamp(value.Value, MediaOptimizeOptions.MinDimension, MediaOptimizeOptions.MaxDimension);
    }

    private static int ResolveQuality(int? value)
    {
        if (value is null)
            return DefaultQuality;

        return Math.Clamp(value.Value, MediaOptimizeOptions.MinQuality, MediaOptimizeOptions.MaxQuality);
    }

    private static WebpEncoder CreateEncoder(int quality) =>
        new()
        {
            Quality = quality,
            FileFormat = WebpFileFormatType.Lossy,
            Method = WebpEncodingMethod.Level4
        };

    private static async Task<OptimizedMediaFile> EncodeAsync(
        Image source,
        string sizeName,
        int maxEdge,
        bool cropSquare,
        WebpEncoder encoder,
        CancellationToken cancellationToken)
    {
        using var clone = source.Clone(ctx =>
        {
            if (cropSquare)
            {
                if (source.Width > maxEdge || source.Height > maxEdge)
                {
                    ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(maxEdge, maxEdge),
                        Mode = ResizeMode.Crop,
                        Sampler = KnownResamplers.Lanczos3
                    });
                }
            }
            else if (source.Width > maxEdge || source.Height > maxEdge)
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(maxEdge, maxEdge),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3
                });
            }
        });

        var output = new MemoryStream();
        try
        {
            await clone.SaveAsync(output, encoder, cancellationToken);
            output.Position = 0;
            return new OptimizedMediaFile(
                sizeName,
                output,
                "image/webp",
                ".webp",
                clone.Width,
                clone.Height,
                output.Length);
        }
        catch
        {
            await output.DisposeAsync();
            throw;
        }
    }
}
