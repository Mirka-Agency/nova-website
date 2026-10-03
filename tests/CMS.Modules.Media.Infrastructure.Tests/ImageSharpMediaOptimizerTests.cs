using CMS.Modules.Media.Application.Imaging;
using CMS.Modules.Media.Infrastructure.Imaging;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CMS.Modules.Media.Infrastructure.Tests;

public class ImageSharpMediaOptimizerTests
{
    [Fact]
    public async Task Optimize_CreatesWebpDisplayAndWordPressSizedDerivatives()
    {
        await using var png = await CreatePngAsync(800, 600);
        var optimizer = new ImageSharpMediaOptimizer();

        using var result = await optimizer.OptimizeAsync(png, "image/png");

        result.UsedOriginalAsDisplay.Should().BeFalse();
        result.Display.Should().NotBeNull();
        result.Display!.ContentType.Should().Be("image/webp");
        result.Display.Width.Should().Be(800);
        result.Display.Height.Should().Be(600);
        result.Display.SizeBytes.Should().BeLessThan(png.Length);

        result.Thumbnail.Should().NotBeNull();
        result.Thumbnail!.Width.Should().Be(150);
        result.Thumbnail.Height.Should().Be(150);

        result.AdditionalSizes.Should().ContainSingle(s => s.SizeName == "medium");
        var medium = result.AdditionalSizes.Single(s => s.SizeName == "medium");
        medium.Width.Should().Be(300);
        medium.Height.Should().Be(225);
        result.AdditionalSizes.Should().NotContain(s => s.SizeName == "large");
    }

    [Fact]
    public async Task Optimize_ScalesImagesLargerThanWordPressThreshold()
    {
        await using var png = await CreatePngAsync(3200, 1800);
        var optimizer = new ImageSharpMediaOptimizer();

        using var result = await optimizer.OptimizeAsync(png, "image/png");

        result.Display.Should().NotBeNull();
        Math.Max(result.Display!.Width, result.Display.Height).Should().Be(ImageSharpMediaOptimizer.MaxFullDimension);
        result.AdditionalSizes.Should().Contain(s => s.SizeName == "large");
        result.AdditionalSizes.Should().Contain(s => s.SizeName == "medium");
    }

    [Fact]
    public async Task Optimize_AppliesCustomMaxDimensionAndQuality()
    {
        await using var png = await CreatePngAsync(1600, 1200);
        var optimizer = new ImageSharpMediaOptimizer();
        var options = new MediaOptimizeOptions
        {
            MaxDisplayDimension = 800,
            Quality = 50
        };

        using var result = await optimizer.OptimizeAsync(png, "image/png", options);

        result.Display.Should().NotBeNull();
        result.Display!.ContentType.Should().Be("image/webp");
        Math.Max(result.Display.Width, result.Display.Height).Should().Be(800);
        result.Display.Width.Should().Be(800);
        result.Display.Height.Should().Be(600);
        result.Display.SizeBytes.Should().BeLessThan(png.Length);
    }

    [Fact]
    public async Task Optimize_AlwaysProducesWebpDisplayForStaticImages()
    {
        await using var png = await CreatePngAsync(64, 64);
        var optimizer = new ImageSharpMediaOptimizer();

        using var result = await optimizer.OptimizeAsync(png, "image/png");

        result.UsedOriginalAsDisplay.Should().BeFalse();
        result.Display.Should().NotBeNull();
        result.Display!.ContentType.Should().Be("image/webp");
        result.Display.FileExtension.Should().Be(".webp");
    }

    private static async Task<MemoryStream> CreatePngAsync(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        var random = new Random(42);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32(
                    (byte)random.Next(256),
                    (byte)random.Next(256),
                    (byte)random.Next(256));
            }
        }

        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;
        return stream;
    }
}
