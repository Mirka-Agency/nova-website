using System.ComponentModel.DataAnnotations;
using CMS.Modules.Blog.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CMS.Modules.Blog.Web.Areas.Admin.ViewModels;

public sealed class PostIndexViewModel
{
    public string? Search { get; init; }
    public PostStatus? Status { get; init; }
    public Guid? CategoryId { get; init; }
    public string? FromLocal { get; init; }
    public string? ToLocal { get; init; }
    public string? Sort { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public IReadOnlyList<SelectListItem> Categories { get; init; } = [];
    public IReadOnlyList<PostListItemViewModel> Items { get; init; } = [];
}

public class PostListItemViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedAtLocal { get; set; } = string.Empty;
}

public class PostFormViewModel
{
    public Guid? Id { get; set; }

    [MaxLength(300, ErrorMessage = "عنوان حداکثر ۳۰۰ نویسه می‌تواند باشد.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300, ErrorMessage = "نامک حداکثر ۳۰۰ نویسه می‌تواند باشد.")]
    public string? Slug { get; set; }

    public string Body { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "توضیحات کوتاه حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string? Excerpt { get; set; }

    [MaxLength(1000, ErrorMessage = "آدرس تصویر شاخص حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string? CoverImageUrl { get; set; }

    [MaxLength(300, ErrorMessage = "متن جایگزین تصویر شاخص حداکثر ۳۰۰ نویسه می‌تواند باشد.")]
    public string? CoverImageAlt { get; set; }

    [MaxLength(1000, ErrorMessage = "آدرس ویدیوی شاخص حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string? CoverVideoUrl { get; set; }

    public Guid? CategoryId { get; set; }

    public string? AuthorUserId { get; set; }

    public bool Publish { get; set; }

    [Display(Name = "تاریخ انتشار")]
    [MaxLength(32, ErrorMessage = "تاریخ انتشار نامعتبر است.")]
    public string? PublishedAtLocal { get; set; }

    [MaxLength(200, ErrorMessage = "عنوان متا حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string? MetaTitle { get; set; }

    [MaxLength(500, ErrorMessage = "توضیحات متا حداکثر ۵۰۰ نویسه می‌تواند باشد.")]
    public string? MetaDescription { get; set; }

    [MaxLength(500, ErrorMessage = "کلمات کلیدی حداکثر ۵۰۰ نویسه می‌تواند باشد.")]
    public string? SeoKeywords { get; set; }

    [MaxLength(1000, ErrorMessage = "آدرس کنونیکال حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string? CanonicalUrl { get; set; }

    [MaxLength(200, ErrorMessage = "عنوان Open Graph حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string? OgTitle { get; set; }

    [MaxLength(500, ErrorMessage = "توضیحات Open Graph حداکثر ۵۰۰ نویسه می‌تواند باشد.")]
    public string? OgDescription { get; set; }

    [MaxLength(1000, ErrorMessage = "آدرس تصویر Open Graph حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string? OgImageUrl { get; set; }

    public CMS.Modules.Seo.Web.Areas.Admin.ViewModels.SeoEditorFieldsViewModel Seo { get; set; } = new();

    public List<PostFaqItemViewModel> FaqItems { get; set; } = [];

    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
    public IReadOnlyList<SelectListItem> Authors { get; set; } = [];
}

public class PostFaqItemViewModel
{
    [MaxLength(500, ErrorMessage = "سوال حداکثر ۵۰۰ نویسه می‌تواند باشد.")]
    public string Question { get; set; } = string.Empty;

    [MaxLength(5000, ErrorMessage = "پاسخ حداکثر ۵۰۰۰ نویسه می‌تواند باشد.")]
    public string Answer { get; set; } = string.Empty;
}

public class CategoryListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int PostCount { get; set; }
    public string? ImageUrl { get; set; }
}

public class CategoryFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "نام الزامی است.")]
    [MaxLength(200, ErrorMessage = "نام حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "نامک حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string? Slug { get; set; }

    public string? Description { get; set; }

    [MaxLength(1000, ErrorMessage = "آدرس تصویر حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string? ImageUrl { get; set; }
}
