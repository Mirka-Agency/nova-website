using CMS.Modules.Comments.Application.Import;
using CMS.Modules.Comments.Domain.Enums;
using FluentAssertions;

namespace CMS.Modules.Comments.Application.Tests;

public class CommentUrlMatcherTests
{
    private readonly CommentUrlMatcher _matcher = new();

    private static IReadOnlyList<CommentTargetCandidate> Catalog() =>
    [
        new(CommentTargetType.BlogPost, Guid.Parse("11111111-1111-1111-1111-111111111111"), "Post A", "/sample-post"),
        new(CommentTargetType.Event, Guid.Parse("22222222-2222-2222-2222-222222222222"), "Event A", "/event/my-event"),
        new(CommentTargetType.Product, Guid.Parse("33333333-3333-3333-3333-333333333333"), "Product A", "/shop/widget"),
        new(CommentTargetType.ProductCategory, Guid.Parse("44444444-4444-4444-4444-444444444444"), "Cat A", "/shop/category/tools")
    ];

    [Theory]
    [InlineData("https://Old.Example.com/sample-post/", "/sample-post")]
    [InlineData("http://old.example.com/sample-post", "/sample-post")]
    [InlineData("/sample-post/", "/sample-post")]
    [InlineData("https://old.example.com/blog/sample-post/", "/blog/sample-post")]
    public void NormalizePath_ignores_scheme_host_case_and_trailing_slash(string input, string expected)
    {
        var path = _matcher.NormalizePath(input, ["old.example.com"]);
        path.Should().Be(expected);
    }

    [Fact]
    public void Match_maps_root_slug_to_blog_post()
    {
        var result = _matcher.Match(
            "https://legacy.test/sample-post/",
            Catalog(),
            ["legacy.test"]);

        result.Kind.Should().Be(CommentUrlMatchKind.Exact);
        result.Target!.TargetType.Should().Be(CommentTargetType.BlogPost);
        result.Target.TargetId.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public void Match_treats_blog_prefix_as_equivalent_to_root_slug()
    {
        var result = _matcher.Match(
            "https://legacy.test/blog/sample-post",
            Catalog(),
            ["legacy.test"]);

        result.Kind.Should().Be(CommentUrlMatchKind.Exact);
        result.Target!.TargetType.Should().Be(CommentTargetType.BlogPost);
    }

    [Fact]
    public void Match_maps_event_and_shop_paths()
    {
        _matcher.Match("https://x.test/events/my-event/", Catalog(), []).Kind
            .Should().Be(CommentUrlMatchKind.Exact);

        _matcher.Match("https://x.test/shop/widget", Catalog(), []).Target!.TargetType
            .Should().Be(CommentTargetType.Product);

        _matcher.Match("https://x.test/shop/category/tools/", Catalog(), []).Target!.TargetType
            .Should().Be(CommentTargetType.ProductCategory);
    }

    [Fact]
    public void Match_returns_none_when_url_unknown()
    {
        var result = _matcher.Match("https://x.test/missing-page/", Catalog(), []);
        result.Kind.Should().Be(CommentUrlMatchKind.None);
        result.Target.Should().BeNull();
    }

    [Theory]
    [InlineData("https://old.test/2023/05/10/sample-post/")]
    [InlineData("https://old.test/category/health/sample-post/")]
    [InlineData("https://old.test/fa/sample-post")]
    public void Match_maps_wordpress_date_and_category_permalinks_by_slug(string url)
    {
        var result = _matcher.Match(url, Catalog(), []);
        result.Kind.Should().Be(CommentUrlMatchKind.Exact);
        result.Target!.TargetType.Should().Be(CommentTargetType.BlogPost);
        result.Target.TargetId.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public void Match_returns_ambiguous_when_multiple_targets_share_path()
    {
        var catalog = new List<CommentTargetCandidate>
        {
            new(CommentTargetType.BlogPost, Guid.NewGuid(), "A", "/dup"),
            new(CommentTargetType.BlogPost, Guid.NewGuid(), "B", "/dup")
        };

        var result = _matcher.Match("https://x.test/dup/", catalog, []);
        result.Kind.Should().Be(CommentUrlMatchKind.Ambiguous);
        result.Target.Should().BeNull();
        result.Candidates.Should().HaveCount(2);
    }
}
