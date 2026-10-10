using CMS.Application.Common.Paging;
using CMS.Modules.Blog.Domain.Enums;

namespace CMS.Modules.Blog.Application.Posts;

public sealed class PostListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;
    public string? Search { get; init; }
    public PostStatus? Status { get; init; }
    public Guid? CategoryId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public string? Sort { get; init; }

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize =>
        PageSize < 1 ? PagedRequest.DefaultPageSize : Math.Min(PageSize, PagedRequest.MaxPageSize);

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}

public sealed record PostListItemDto(
    Guid Id,
    string Title,
    string Slug,
    PostStatus Status,
    string? CategoryName,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc);

public sealed record PostDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string? Excerpt,
    PostStatus Status,
    string? CoverImageUrl,
    string? CoverImageAlt,
    string? CoverVideoUrl,
    Guid? CategoryId,
    string? AuthorUserId,
    string? AuthorDisplayName,
    DateTime? PublishedAtUtc,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string? FaqJson,
    string? VideoSchemaJson,
    DateTime CreatedAtUtc);

public static class PostDraftDefaults
{
    public const string Title = "پیش‌نویس";
}

public sealed record SavePostCommand(
    string Title,
    string? Slug,
    string Body,
    string? Excerpt,
    Guid? CategoryId,
    string? CoverImageUrl,
    string? CoverImageAlt,
    string? CoverVideoUrl,
    bool? Publish,
    DateTime? PublishedAtUtc,
    string? AuthorUserId,
    string? AuthorDisplayName,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string? FaqJson,
    string? VideoSchemaJson);
