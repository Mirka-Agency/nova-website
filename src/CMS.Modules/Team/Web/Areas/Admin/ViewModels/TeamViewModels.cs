using System.ComponentModel.DataAnnotations;
using CMS.Modules.Team.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CMS.Modules.Team.Web.Areas.Admin.ViewModels;

public sealed class TeamItemIndexViewModel
{
    public string? Search { get; init; }
    public TeamStatus? Status { get; init; }
    public Guid? CategoryId { get; init; }
    public string? FromLocal { get; init; }
    public string? ToLocal { get; init; }
    public string? Sort { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public IReadOnlyList<SelectListItem> Categories { get; init; } = [];
    public IReadOnlyList<TeamItemListItemViewModel> Items { get; init; } = [];
}

public class TeamItemListItemViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public string? CategoryName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedAtLocal { get; set; } = string.Empty;
}

public class TeamItemFormViewModel
{
    public Guid? Id { get; set; }

    [MaxLength(300, ErrorMessage = "عنوان حداکثر ۳۰۰ نویسه می‌تواند باشد.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300, ErrorMessage = "عنوان فرعی حداکثر ۳۰۰ نویسه می‌تواند باشد.")]
    public string? Subtitle { get; set; }

    [MaxLength(300, ErrorMessage = "نامک حداکثر ۳۰۰ نویسه می‌تواند باشد.")]
    public string? Slug { get; set; }

    public string Body { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "توضیحات کوتاه حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string? Excerpt { get; set; }

    [MaxLength(2000, ErrorMessage = "نکات برجسته حداکثر ۲۰۰۰ نویسه می‌تواند باشد.")]
    public string? Highlights { get; set; }

    public List<TeamSpecialtyPathItemViewModel> SpecialtyPathItems { get; set; } = [];

    public List<TeamEducationPathItemViewModel> EducationPathItems { get; set; } = [];

    public List<TeamScientificActivityItemViewModel> ScientificActivityItems { get; set; } = [];

    public List<TeamFaqItemViewModel> FaqItems { get; set; } = [];

    [MaxLength(1000, ErrorMessage = "آدرس تصویر شاخص حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string? CoverImageUrl { get; set; }

    [MaxLength(300, ErrorMessage = "متن جایگزین تصویر شاخص حداکثر ۳۰۰ نویسه می‌تواند باشد.")]
    public string? CoverImageAlt { get; set; }

    [MaxLength(1000, ErrorMessage = "آدرس تصویر آواتار حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string? AvatarImageUrl { get; set; }

    public Guid? CategoryId { get; set; }

    public string? AuthorUserId { get; set; }

    public bool Publish { get; set; } = true;

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

    public IReadOnlyList<SelectListItem> Categories { get; set; } = [];
    public IReadOnlyList<SelectListItem> Authors { get; set; } = [];
}

public class TeamSpecialtyPathItemViewModel
{
    [MaxLength(200, ErrorMessage = "عنوان حوزه فعالیت حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "توضیح حوزه فعالیت حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string Text { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "آدرس آیکون حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string? IconUrl { get; set; }
}

public class TeamEducationPathItemViewModel
{
    [MaxLength(32, ErrorMessage = "سال حداکثر ۳۲ نویسه می‌تواند باشد.")]
    public string Year { get; set; } = string.Empty;

    [MaxLength(300, ErrorMessage = "عنوان مسیر تخصصی حداکثر ۳۰۰ نویسه می‌تواند باشد.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300, ErrorMessage = "محل مسیر تخصصی حداکثر ۳۰۰ نویسه می‌تواند باشد.")]
    public string Place { get; set; } = string.Empty;
}

public class TeamScientificActivityItemViewModel
{
    [MaxLength(1000, ErrorMessage = "فعالیت علمی حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string Text { get; set; } = string.Empty;
}

public class TeamFaqItemViewModel
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
    public int TeamItemCount { get; set; }
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
