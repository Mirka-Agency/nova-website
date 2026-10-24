using CMS.Modules.Comments.Domain.Entities;
using CMS.Modules.Comments.Domain.Enums;
using FluentAssertions;

namespace CMS.Modules.Comments.Application.Tests;

public class CommentImportDomainTests
{
    [Fact]
    public void Import_preserves_published_date_status_parent_and_source_keys()
    {
        var targetId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var published = new DateTime(2020, 5, 1, 12, 0, 0, DateTimeKind.Utc);

        var comment = Comment.Import(
            CommentTargetType.BlogPost,
            targetId,
            "Sample Post",
            "Ali",
            "ali@example.com",
            "Body text",
            CommentStatus.Approved,
            published,
            Comment.WordPressSourceSystem,
            "wp-99",
            "https://old.example.com/sample-post/",
            parentId);

        comment.PublishedAtUtc.Should().Be(published);
        comment.CreatedAtUtc.Should().Be(published);
        comment.Status.Should().Be(CommentStatus.Approved);
        comment.ParentId.Should().Be(parentId);
        comment.SourceSystem.Should().Be("wordpress");
        comment.SourceExternalId.Should().Be("wp-99");
        comment.SourcePageUrl.Should().Contain("sample-post");
        comment.TargetId.Should().Be(targetId);
    }

    [Theory]
    [InlineData(CommentStatus.Approved)]
    [InlineData(CommentStatus.Pending)]
    [InlineData(CommentStatus.Rejected)]
    public void Import_applies_comment_approved_status_exactly(CommentStatus status)
    {
        var comment = Comment.Import(
            CommentTargetType.BlogPost,
            Guid.NewGuid(),
            "T",
            "A",
            null,
            "body",
            status,
            new DateTime(2023, 5, 10, 8, 30, 0, DateTimeKind.Utc),
            Comment.WordPressSourceSystem,
            "101",
            "https://old.example.com/sample-post/");

        comment.Status.Should().Be(status);
    }

    [Fact]
    public void Import_source_keys_enable_idempotent_identity()
    {
        var a = Comment.Import(
            CommentTargetType.BlogPost,
            Guid.NewGuid(),
            "T",
            "A",
            null,
            "b",
            CommentStatus.Pending,
            DateTime.UtcNow.AddDays(-10),
            Comment.WordPressSourceSystem,
            "42",
            "https://x.test/a/");

        var b = Comment.Import(
            CommentTargetType.BlogPost,
            Guid.NewGuid(),
            "T",
            "A",
            null,
            "b",
            CommentStatus.Pending,
            DateTime.UtcNow.AddDays(-10),
            Comment.WordPressSourceSystem,
            "42",
            "https://x.test/a/");

        a.SourceExternalId.Should().Be(b.SourceExternalId);
        a.SourceSystem.Should().Be(b.SourceSystem);
        a.Id.Should().NotBe(b.Id);
    }

    [Fact]
    public void SetParent_links_reply_to_parent()
    {
        var parent = Comment.Import(
            CommentTargetType.BlogPost,
            Guid.NewGuid(),
            "T",
            "P",
            null,
            "parent",
            CommentStatus.Approved,
            DateTime.UtcNow.AddDays(-2),
            Comment.WordPressSourceSystem,
            "1",
            "https://x.test/a/");

        var child = Comment.Import(
            CommentTargetType.BlogPost,
            parent.TargetId,
            "T",
            "C",
            null,
            "child",
            CommentStatus.Approved,
            DateTime.UtcNow.AddDays(-1),
            Comment.WordPressSourceSystem,
            "2",
            "https://x.test/a/");

        child.SetParent(parent.Id);
        child.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public void SetParent_repairs_missing_parent_after_flat_import()
    {
        var targetId = Guid.NewGuid();
        var parent = Comment.Import(
            CommentTargetType.BlogPost,
            targetId,
            "T",
            "P",
            null,
            "parent",
            CommentStatus.Approved,
            DateTime.UtcNow.AddDays(-2),
            Comment.WordPressSourceSystem,
            "10",
            "https://x.test/a/");

        // Simulate same-batch import that failed to resolve parent (ParentId left null).
        var child = Comment.Import(
            CommentTargetType.BlogPost,
            targetId,
            "T",
            "C",
            null,
            "reply",
            CommentStatus.Approved,
            DateTime.UtcNow.AddDays(-1),
            Comment.WordPressSourceSystem,
            "11",
            "https://x.test/a/",
            parentId: null);

        child.ParentId.Should().BeNull();
        child.SetParent(parent.Id);
        child.ParentId.Should().Be(parent.Id);
    }
}
