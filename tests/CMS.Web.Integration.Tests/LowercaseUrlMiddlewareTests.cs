using CMS.Web.Middleware;
using FluentAssertions;

namespace CMS.Web.Integration.Tests;

public class LowercaseUrlMiddlewareTests
{
    [Theory]
    [InlineData("/Blog/Hello", "/blog/hello/")]
    [InlineData("/blog/hello", "/blog/hello/")]
    [InlineData("/blog/hello/", null)]
    [InlineData("/blog//hello", "/blog/hello/")]
    [InlineData("/blog//hello///", "/blog/hello/")]
    [InlineData("//about", "/about/")]
    [InlineData("/about///", "/about/")]
    public void TryCanonicalize_PublicPaths(string input, string? expected)
    {
        var shouldRedirect = LowercaseUrlMiddleware.TryCanonicalize(input, out var canonical);
        if (expected is null)
        {
            shouldRedirect.Should().BeFalse();
            return;
        }

        shouldRedirect.Should().BeTrue();
        canonical.Should().Be(expected);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/api/v1/items")]
    [InlineData("/api//v1/items")]
    [InlineData("/admin/dashboard")]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/robots.txt")]
    [InlineData("/sitemap.xml")]
    [InlineData("/css/site.css")]
    [InlineData("/file.txt///")]
    public void TryCanonicalize_SkipsApiAdminHealthAndFiles(string input)
    {
        LowercaseUrlMiddleware.TryCanonicalize(input, out _).Should().BeFalse();
    }
}
