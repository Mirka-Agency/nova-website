using CMS.Domain.Exceptions;
using CMS.Modules.Media.Domain.Entities;
using CMS.Modules.Media.Domain.ValueObjects;
using FluentAssertions;

namespace CMS.Modules.Media.Domain.Tests;

public class MediaAssetTests
{
    [Fact]
    public void Create_KeepsOriginalSeparateFromOptimizedDisplay()
    {
        var original = File("photo.jpg", "image/jpeg", 2000, "media/library/orig.jpg");
        var display = File("photo-full.webp", "image/webp", 800, "media/library/full.webp", 1200, 800);
        var thumb = File("photo-thumbnail.webp", "image/webp", 120, "media/library/thumb.webp", 150, 150);

        var asset = MediaAsset.Create(display, original, thumbnail: thumb);

        asset.ObjectKey.Should().Be(display.ObjectKey);
        asset.OriginalObjectKey.Should().Be(original.ObjectKey);
        asset.HasSeparateOriginal.Should().BeTrue();
        asset.ThumbnailObjectKey.Should().Be(thumb.ObjectKey);
        asset.GetAllStorageKeys().Should().BeEquivalentTo([
            display.ObjectKey,
            original.ObjectKey,
            thumb.ObjectKey
        ]);
    }

    [Fact]
    public void ReplaceDisplay_DoesNotDropOriginal()
    {
        var original = File("photo.jpg", "image/jpeg", 2000, "media/library/orig.jpg");
        var display = File("photo-full.webp", "image/webp", 800, "media/library/full.webp");
        var asset = MediaAsset.Create(display, original);

        var edited = File("photo-edited.webp", "image/webp", 500, "media/library/edited.webp");
        var stale = asset.ReplaceDisplay(edited);

        stale.Should().Contain(display.ObjectKey);
        stale.Should().NotContain(original.ObjectKey);
        asset.OriginalObjectKey.Should().Be(original.ObjectKey);
        asset.ObjectKey.Should().Be(edited.ObjectKey);
        asset.HasSeparateOriginal.Should().BeTrue();
    }

    [Fact]
    public void ReplaceDisplay_CapturesCurrentFileAsOriginal_WhenMissing()
    {
        var same = File("legacy.jpg", "image/jpeg", 1500, "media/library/legacy.jpg");
        var asset = MediaAsset.Create(same, same);
        asset.HasSeparateOriginal.Should().BeFalse();

        var edited = File("legacy-full.webp", "image/webp", 400, "media/library/new.webp");
        var stale = asset.ReplaceDisplay(edited);

        stale.Should().NotContain(same.ObjectKey);
        asset.OriginalObjectKey.Should().Be(same.ObjectKey);
        asset.ObjectKey.Should().Be(edited.ObjectKey);
        asset.HasSeparateOriginal.Should().BeTrue();
    }

    [Fact]
    public void RestoreOriginal_SwitchesDisplayBackAndReturnsDerivatives()
    {
        var original = File("photo.jpg", "image/jpeg", 2000, "media/library/orig.jpg");
        var display = File("photo-full.webp", "image/webp", 800, "media/library/full.webp");
        var thumb = File("photo-thumbnail.webp", "image/webp", 120, "media/library/thumb.webp");
        var asset = MediaAsset.Create(display, original, thumbnail: thumb);

        var stale = asset.RestoreOriginal();

        stale.Should().BeEquivalentTo([display.ObjectKey, thumb.ObjectKey]);
        asset.ObjectKey.Should().Be(original.ObjectKey);
        asset.HasSeparateOriginal.Should().BeFalse();
        asset.ThumbnailObjectKey.Should().BeNull();
    }

    [Fact]
    public void RestoreOriginal_WhenAlreadyOriginal_Throws()
    {
        var same = File("photo.jpg", "image/jpeg", 2000, "media/library/orig.jpg");
        var asset = MediaAsset.Create(same, same);

        var act = () => asset.RestoreOriginal();
        act.Should().Throw<DomainException>();
    }

    private static MediaFileRef File(
        string name,
        string contentType,
        long size,
        string key,
        int? width = null,
        int? height = null) =>
        new(name, contentType, size, key, width, height);
}
