using System.ComponentModel.DataAnnotations;

namespace CMS.Modules.Voices.Web.Areas.Admin.ViewModels;

public sealed class VoiceItemIndexViewModel
{
    public string? Search { get; init; }
    public bool? IsPublished { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public IReadOnlyList<VoiceItemListItemViewModel> Items { get; init; } = [];
}

public sealed class VoiceItemListItemViewModel
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
}

public sealed class VoiceItemFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "نام کاربر الزامی است.")]
    [MaxLength(200, ErrorMessage = "نام کاربر حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(300, ErrorMessage = "زیرعنوان حداکثر ۳۰۰ نویسه می‌تواند باشد.")]
    public string? Subtitle { get; set; }

    [MaxLength(2000, ErrorMessage = "توضیحات حداکثر ۲۰۰۰ نویسه می‌تواند باشد.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "فایل صوتی الزامی است.")]
    [MaxLength(1000, ErrorMessage = "آدرس فایل صوتی حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string AudioUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsPublished { get; set; } = true;
}
