using System.ComponentModel.DataAnnotations;

namespace CMS.Modules.Team.Web.Areas.Admin.ViewModels;

public sealed class SaveTeamItemApiRequest
{
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Slug { get; set; }

    [Required(AllowEmptyStrings = true)]
    public string Body { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Excerpt { get; set; }

    [MaxLength(1000)]
    public string? CoverImageUrl { get; set; }

    public Guid? CategoryId { get; set; }

    public string? AuthorUserId { get; set; }

    public string? AuthorDisplayName { get; set; }

    public bool Publish { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

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
}

public sealed class SaveTeamItemApiResponse
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
}
