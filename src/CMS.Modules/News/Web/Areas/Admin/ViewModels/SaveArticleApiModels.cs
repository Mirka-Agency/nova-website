using System.ComponentModel.DataAnnotations;
using CMS.Modules.News.Domain.Enums;

namespace CMS.Modules.News.Web.Areas.Admin.ViewModels;

public sealed class SaveArticleApiRequest
{
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Slug { get; set; }

    [Required(AllowEmptyStrings = true)]
    public string Body { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Excerpt { get; set; }

    public ArticleKind Kind { get; set; } = ArticleKind.News;

    [MaxLength(1000)]
    public string? CoverImageUrl { get; set; }

    public string? GalleryJson { get; set; }

    [MaxLength(1000)]
    public string? AttachmentUrl { get; set; }

    [MaxLength(300)]
    public string? AttachmentFileName { get; set; }

    public Guid? CategoryId { get; set; }

    public string? AuthorUserId { get; set; }

    public string? AuthorDisplayName { get; set; }

    /// <summary>Null keeps the current publish state (used by autosave).</summary>
    public bool? Publish { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public DateTime? EventStartAtUtc { get; set; }

    public DateTime? EventEndAtUtc { get; set; }

    [MaxLength(300)]
    public string? Location { get; set; }

    public string? EventInfoJson { get; set; }

    [MaxLength(200)]
    public string? MetaTitle { get; set; }

    [MaxLength(500)]
    public string? MetaDescription { get; set; }

    [MaxLength(500)]
    public string? SeoKeywords { get; set; }

    [MaxLength(1000)]
    public string? CanonicalUrl { get; set; }

    [MaxLength(200)]
    public string? OgTitle { get; set; }

    [MaxLength(500)]
    public string? OgDescription { get; set; }

    [MaxLength(1000)]
    public string? OgImageUrl { get; set; }

    public string? FocusKeyword { get; set; }
    public bool RobotsIndex { get; set; } = true;
    public bool RobotsFollow { get; set; } = true;
    public string? SchemaType { get; set; }
    public int? SeoScore { get; set; }
}

public sealed class SaveArticleApiResponse
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
}
