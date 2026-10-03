using CMS.Application.Common.Paging;
using CMS.Modules.News.Domain.Enums;

namespace CMS.Modules.News.Application.Articles;

public sealed class ArticleListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;
    public string? Search { get; init; }
    public ArticleStatus? Status { get; init; }
    public ArticleKind? Kind { get; init; }
    public Guid? CategoryId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
    public DateTime? EventFromUtc { get; init; }
    public DateTime? EventToUtc { get; init; }
    public string? Sort { get; init; }

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize =>
        PageSize < 1 ? PagedRequest.DefaultPageSize : Math.Min(PageSize, PagedRequest.MaxPageSize);

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}

public sealed record ArticleListItemDto(
    Guid Id,
    string Title,
    string Slug,
    ArticleStatus Status,
    ArticleKind Kind,
    string? CategoryName,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    DateTime? EventStartAtUtc);

public sealed record ArticleDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string? Excerpt,
    ArticleStatus Status,
    ArticleKind Kind,
    string? CoverImageUrl,
    Guid? CategoryId,
    string? AuthorUserId,
    string? AuthorDisplayName,
    DateTime? PublishedAtUtc,
    DateTime? EventStartAtUtc,
    DateTime? EventEndAtUtc,
    string? Location,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    DateTime CreatedAtUtc);

public static class ArticleDraftDefaults
{
    public const string Title = "پیش‌نویس";
}

public sealed record SaveArticleCommand(
    string Title,
    string? Slug,
    string Body,
    string? Excerpt,
    ArticleKind Kind,
    Guid? CategoryId,
    string? CoverImageUrl,
    bool Publish,
    DateTime? PublishedAtUtc,
    DateTime? EventStartAtUtc,
    DateTime? EventEndAtUtc,
    string? Location,
    string? AuthorUserId,
    string? AuthorDisplayName,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl);
