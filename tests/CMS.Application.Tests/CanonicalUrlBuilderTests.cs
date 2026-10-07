using CMS.Application.Seo;
using FluentAssertions;

namespace CMS.Application.Tests;

public class CanonicalUrlBuilderTests
{
    [Fact]
    public void Build_FromRequest_LowercasesPath_AndDropsTrailingSlash()
    {
        var url = CanonicalUrlBuilder.Build(
            configured: null,
            scheme: "https",
            host: "example.com",
            pathBase: "",
            path: "/Blog/Hello/",
            pageQueryValue: null);

        url.Should().Be("https://example.com/blog/hello");
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

        url.Should().Be("https://example.com/blog?page=2");
    }

    [Fact]
    public void Build_DropsPageOne_AndOtherQueryNoise()
    {
        var url = CanonicalUrlBuilder.Build(
            configured: "https://example.com/blog/post?utm_source=x&page=1",
            scheme: "https",
            host: "example.com");

        url.Should().Be("https://example.com/blog/post");
    }

    [Fact]
    public void Build_RewritesLegacyPublicPaths()
    {
        CanonicalUrlBuilder.Build("/news/foo", "https", "example.com")
            .Should().Be("https://example.com/education-articles/foo");

        CanonicalUrlBuilder.Build("/Teams/bar", "https", "example.com")
            .Should().Be("https://example.com/doctors/bar");

        CanonicalUrlBuilder.Build("/events/baz", "https", "example.com")
            .Should().Be("https://example.com/event/baz");
    }

    [Fact]
    public void Build_SameHostAbsolute_UsesRequestScheme()
    {
        var url = CanonicalUrlBuilder.Build(
            configured: "http://example.com/Services/Item/",
            scheme: "https",
            host: "example.com");

        url.Should().Be("https://example.com/services/item");
    }

    [Fact]
    public void Build_ExternalAbsolute_KeepsForeignHost()
    {
        var url = CanonicalUrlBuilder.Build(
            configured: "https://other.test/Path/A/",
            scheme: "https",
            host: "example.com");

        url.Should().Be("https://other.test/path/a");
    }

    [Fact]
    public void ForContent_UsesFallbackWhenCanonicalEmpty()
    {
        CanonicalUrlBuilder.ForContent(null, "/blog/my-post", "https", "example.com")
            .Should().Be("https://example.com/blog/my-post");
    }
}
