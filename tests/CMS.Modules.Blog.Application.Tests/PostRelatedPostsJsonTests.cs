using CMS.Modules.Blog.Application.Posts;
using FluentAssertions;

namespace CMS.Modules.Blog.Application.Tests;

public class PostRelatedPostsJsonTests
{
    [Fact]
    public void WithId_prepends_and_is_idempotent()
    {
        var a = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var b = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var json = PostRelatedPostsJson.Serialize([b]);
        var withA = PostRelatedPostsJson.WithId(json, a);

        PostRelatedPostsJson.Parse(withA).Should().Equal(a, b);
        PostRelatedPostsJson.WithId(withA, a).Should().Be(withA);
    }

    [Fact]
    public void WithoutId_removes_target()
    {
        var a = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var b = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var json = PostRelatedPostsJson.Serialize([a, b]);
        var withoutA = PostRelatedPostsJson.WithoutId(json, a);

        PostRelatedPostsJson.Parse(withoutA).Should().Equal(b);
        PostRelatedPostsJson.WithoutId(withoutA, a).Should().Be(withoutA);
    }

    [Fact]
    public void WithId_respects_max_items()
    {
        var ids = Enumerable.Range(0, PostRelatedPostsJson.MaxItems)
            .Select(i => Guid.Parse($"00000000-0000-0000-0000-{i + 1:D12}"))
            .ToList();
        var json = PostRelatedPostsJson.Serialize(ids);
        var extra = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var result = PostRelatedPostsJson.WithId(json, extra);
        PostRelatedPostsJson.Parse(result).Should().HaveCount(PostRelatedPostsJson.MaxItems);
        PostRelatedPostsJson.Parse(result).Should().NotContain(extra);
    }
}
