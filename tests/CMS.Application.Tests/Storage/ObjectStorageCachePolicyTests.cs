using CMS.Application.Storage;
using FluentAssertions;

namespace CMS.Application.Tests.Storage;

public sealed class ObjectStorageCachePolicyTests
{
    [Theory]
    [InlineData("media/library/abc123_photo.jpg")]
    [InlineData("blog/covers/abc123_cover.webp")]
    [InlineData("shop/products/abc123_item.png")]
    [InlineData("news/covers/abc123_n.jpg")]
    [InlineData("video/posters/abc123_p.jpg")]
    public void Public_module_keys_get_immutable_year_cache(string key)
    {
        ObjectStorageCachePolicy.ResolveCacheControl(key)
            .Should().Be(ObjectStorageCachePolicy.PublicImmutable);
        ObjectStorageCachePolicy.IsPublicImmutableCandidate(key).Should().BeTrue();
    }

    [Fact]
    public void Form_attachments_get_private_no_store()
    {
        var key = "forms/booking/abc123_resume.pdf";
        ObjectStorageCachePolicy.ResolveCacheControl(key)
            .Should().Be(ObjectStorageCachePolicy.PrivateNoStore);
        ObjectStorageCachePolicy.IsPublicImmutableCandidate(key).Should().BeFalse();
    }

    [Fact]
    public void Unknown_prefixes_leave_cache_control_unset()
    {
        ObjectStorageCachePolicy.ResolveCacheControl("tmp/scratch.bin").Should().BeNull();
        ObjectStorageCachePolicy.IsPublicImmutableCandidate("tmp/scratch.bin").Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("max-age=0")]
    [InlineData("public, max-age=3600")]
    public void Missing_or_wrong_cache_control_needs_backfill(string? existing)
    {
        ObjectStorageCachePolicy
            .HasDesiredCacheControl("media/library/x.jpg", existing)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData("public, max-age=31536000, immutable")]
    [InlineData("public,max-age=31536000,immutable")]
    [InlineData("Public, Max-Age=31536000, Immutable")]
    public void Matching_cache_control_skips_backfill(string existing)
    {
        ObjectStorageCachePolicy
            .HasDesiredCacheControl("media/library/x.jpg", existing)
            .Should().BeTrue();
    }
}
