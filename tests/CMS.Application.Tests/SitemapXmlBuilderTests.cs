using CMS.Application.Seo;
using FluentAssertions;

namespace CMS.Application.Tests;

public class SitemapXmlBuilderTests
{
    [Fact]
    public void Build_Includes_Loc_Lastmod_Changefreq_Priority()
    {
        var xml = SitemapXmlBuilder.Build(
            "https://example.com/",
            [
                new SitemapUrlEntry(
                    "/blog/hello",
                    new DateTime(2026, 3, 15, 12, 30, 0, DateTimeKind.Utc),
                    SitemapChangeFrequency.Weekly,
                    0.7)
            ]);

        xml.Should().Contain("""<loc>https://example.com/blog/hello</loc>""");
        xml.Should().Contain("""<lastmod>2026-03-15T12:30:00Z</lastmod>""");
        xml.Should().Contain("""<changefreq>weekly</changefreq>""");
        xml.Should().Contain("""<priority>0.7</priority>""");
    }

    [Fact]
    public void Build_Deduplicates_Paths_Case_Insensitive()
    {
        var xml = SitemapXmlBuilder.Build(
            "https://example.com",
            [
                new SitemapUrlEntry("/blog/a"),
                new SitemapUrlEntry("/Blog/A"),
                new SitemapUrlEntry("blog/b")
            ]);

        xml.Split("<url>", StringSplitOptions.None).Length.Should().Be(3); // header + 2 urls
        xml.Should().Contain("""<loc>https://example.com/blog/b</loc>""");
    }

    [Fact]
    public void Build_Escapes_Ampersand_In_Url()
    {
        var xml = SitemapXmlBuilder.Build(
            "https://example.com",
            [new SitemapUrlEntry("/search?q=a&b=1")]);

        xml.Should().Contain("""<loc>https://example.com/search?q=a&amp;b=1</loc>""");
    }
}

public class SitemapIndexXmlBuilderTests
{
    [Fact]
    public void Build_Lists_Sub_Sitemap_Locations()
    {
        var xml = SitemapIndexXmlBuilder.Build(
            "https://example.com",
            ["/sitemaps/pages", "/sitemaps/blog", "/sitemaps/shop"]);

        xml.Should().Contain("""<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");
        xml.Should().Contain("""<loc>https://example.com/sitemaps/pages</loc>""");
        xml.Should().Contain("""<loc>https://example.com/sitemaps/blog</loc>""");
        xml.Should().Contain("""<loc>https://example.com/sitemaps/shop</loc>""");
    }
}
