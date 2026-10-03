using System.ComponentModel.DataAnnotations;

namespace CMS.Modules.Honors.Web.Areas.Admin.ViewModels;

public sealed class HonorItemIndexViewModel
{
    public string? Search { get; init; }
    public bool? IsPublished { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public IReadOnlyList<HonorItemListItemViewModel> Items { get; init; } = [];
}

public sealed class HonorItemListItemViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
}

public sealed class HonorItemFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "عنوان الزامی است.")]
    [MaxLength(200, ErrorMessage = "عنوان حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "تصویر الزامی است.")]
    [MaxLength(1000, ErrorMessage = "آدرس تصویر حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(300, ErrorMessage = "متن جایگزین حداکثر ۳۰۰ نویسه می‌تواند باشد.")]
    public string? AltText { get; set; }

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}
