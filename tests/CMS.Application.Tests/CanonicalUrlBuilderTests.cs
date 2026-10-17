using CMS.Application.Seo;
using FluentAssertions;

namespace CMS.Application.Tests;

public class CanonicalUrlBuilderTests
{
    [Fact]
    public void Build_FromRequest_LowercasesPath_AndAddsTrailingSlash()
    {
        var url = CanonicalUrlBuilder.Build(
            configured: null,
            scheme: "https",
            host: "example.com",
            pathBase: "",
            path: "/Blog/Hello/",
            pageQueryValue: null);

        url.Should().Be("https://example.com/blog/hello/");
    }

    [Fact]
    public void Build_KeepsPageQuery_WhenGreaterThanOne()
    {
        var url = CanonicalUrlBuilder.Build(
            configured: null,
            scheme: "https",
            host: "example.com",
            pathBase: "",
            path: "/blog",
            pageQueryValue: "2");

        url.Should().Be("https://example.com/blog/?page=2");
    }

    [Fact]
    public void PagedPath_AddsTrailingSlash_AndPageQueryWhenGreaterThanOne()
    {
        CanonicalUrlBuilder.PagedPath("/blog", 1).Should().Be("/blog/");
        CanonicalUrlBuilder.PagedPath("/blog", 2).Should().Be("/blog/?page=2");
        CanonicalUrlBuilder.PagedPath("/shop/category/Herbs", 3)
            .Should().Be("/shop/category/herbs/?page=3");
    }

    [Fact]
    public void Build_DropsPageOne_AndOtherQueryNoise()
    {
        var url = CanonicalUrlBuilder.Build(
            configured: "https://example.com/blog/post?utm_source=x&page=1",
            scheme: "https",
            host: "example.com");

        url.Should().Be("https://example.com/blog/post/");
    }

    [Fact]
    public void Build_RewritesLegacyPublicPaths()
    {
        CanonicalUrlBuilder.Build("/news/foo", "https", "example.com")
            .Should().Be("https://example.com/education-articles/foo/");

        CanonicalUrlBuilder.Build("/Teams/bar", "https", "example.com")
            .Should().Be("https://example.com/doctors/bar/");

        CanonicalUrlBuilder.Build("/events/baz", "https", "example.com")
            .Should().Be("https://example.com/event/baz/");

        CanonicalUrlBuilder.Build("/Home/Privacy", "https", "example.com")
            .Should().Be("https://example.com/privacy/");
    }

    [Fact]
    public void Build_SameHostAbsolute_UsesRequestScheme()
    {
        var url = CanonicalUrlBuilder.Build(
            configured: "http://example.com/Services/Item/",
            scheme: "https",
            host: "example.com");

        url.Should().Be("https://example.com/services/item/");
    }

    [Fact]
    public void Build_ExternalAbsolute_KeepsForeignHost()
    {
        var url = CanonicalUrlBuilder.Build(
            configured: "https://other.test/Path/A/",
            scheme: "https",
            host: "example.com");

        url.Should().Be("https://other.test/path/a/");
    }

    [Fact]
    public void ForContent_UsesFallbackWhenCanonicalEmpty()
    {
        CanonicalUrlBuilder.ForContent(null, "/blog/my-post", "https", "example.com")
            .Should().Be("https://example.com/blog/my-post/");
    }

    [Fact]
    public void Build_Root_StaysSingleSlash()
    {
        CanonicalUrlBuilder.Build("/", "https", "example.com")
            .Should().Be("https://example.com/");
    }

    [Fact]
    public void ContentPath_PrefersCanonical_AndNormalizes()
    {
        CanonicalUrlBuilder.ContentPath("https://old.example/News/Foo", "/education-articles/fallback")
            .Should().Be("/education-articles/foo/");

        CanonicalUrlBuilder.ContentPath(null, "/Blog/My-Post")
            .Should().Be("/blog/my-post/");
    }

    [Fact]
    public void NormalizePublicPath_AppliesLegacyRewrites()
    {
        CanonicalUrlBuilder.NormalizePublicPath("/Teams/Dr-X")
            .Should().Be("/doctors/dr-x/");
    }
}
