using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CMS.Web.Areas.Admin.ViewModels;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string AvatarDisplayUrl { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool IsLockedOut { get; set; }
    public int AccessFailedCount { get; set; }
    public bool HasLoginRestriction => IsLockedOut || AccessFailedCount > 0;
}

public class CreateUserViewModel
{
    [Required(ErrorMessage = "ایمیل الزامی است.")]
    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    [Display(Name = "ایمیل")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "نام کامل")]
    [StringLength(200, ErrorMessage = "نام کامل حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string? FullName { get; set; }

    [Display(Name = "آواتار")]
    [StringLength(2000, ErrorMessage = "آدرس آواتار حداکثر ۲۰۰۰ نویسه می‌تواند باشد.")]
    public string? AvatarUrl { get; set; }

    [Display(Name = "شماره موبایل")]
    [RegularExpression(@"^$|^09\d{9}$", ErrorMessage = "شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود.")]
    public string? PhoneNumber { get; set; }

    public const string DefaultPassword = "Admin123!";

    [Required(ErrorMessage = "رمز عبور الزامی است.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "رمز عبور باید حداقل ۸ کاراکتر باشد.")]
    [DataType(DataType.Password)]
    [Display(Name = "رمز عبور")]
    public string Password { get; set; } = DefaultPassword;

    [Display(Name = "نقش‌ها")]
    public List<string> SelectedRoles { get; set; } = [];

    public IEnumerable<SelectListItem> AvailableRoles { get; set; } = [];
}

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "ایمیل")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "نام کامل")]
    [StringLength(200, ErrorMessage = "نام کامل حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string? FullName { get; set; }

    [Display(Name = "آواتار")]
    [StringLength(2000, ErrorMessage = "آدرس آواتار حداکثر ۲۰۰۰ نویسه می‌تواند باشد.")]
    public string? AvatarUrl { get; set; }

    [Display(Name = "شماره موبایل")]
    [RegularExpression(@"^$|^09\d{9}$", ErrorMessage = "شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود.")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "نقش‌ها")]
    public List<string> SelectedRoles { get; set; } = [];

    public IEnumerable<SelectListItem> AvailableRoles { get; set; } = [];

    [Display(Name = "قفل حساب")]
    public bool IsLockedOut { get; set; }

    public int AccessFailedCount { get; set; }

    public bool HasLoginRestriction => IsLockedOut || AccessFailedCount > 0;
}

public class AdminSetUserPasswordViewModel
{
    public string UserId { get; set; } = string.Empty;

    [Display(Name = "ایمیل")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز عبور جدید الزامی است.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "رمز عبور باید حداقل ۸ کاراکتر باشد.")]
    [DataType(DataType.Password)]
    [Display(Name = "رمز عبور جدید")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "تکرار رمز عبور الزامی است.")]
    [DataType(DataType.Password)]
    [Display(Name = "تکرار رمز عبور جدید")]
    [Compare(nameof(NewPassword), ErrorMessage = "رمز عبور جدید و تکرار آن یکسان نیستند.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
