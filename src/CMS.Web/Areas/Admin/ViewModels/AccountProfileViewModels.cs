using System.ComponentModel.DataAnnotations;

namespace CMS.Web.Areas.Admin.ViewModels;

public class ProfileEditViewModel
{
    [Display(Name = "ایمیل")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "نام کامل الزامی است.")]
    [Display(Name = "نام کامل")]
    [StringLength(200, ErrorMessage = "نام کامل حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "آواتار")]
    [StringLength(2000, ErrorMessage = "آدرس آواتار حداکثر ۲۰۰۰ نویسه می‌تواند باشد.")]
    public string? AvatarUrl { get; set; }

    [Display(Name = "شماره موبایل")]
    [RegularExpression(@"^$|^09\d{9}$", ErrorMessage = "شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود.")]
    public string? PhoneNumber { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "رمز عبور فعلی الزامی است.")]
    [DataType(DataType.Password)]
    [Display(Name = "رمز عبور فعلی")]
    public string CurrentPassword { get; set; } = string.Empty;

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

public class AdminUserMenuViewModel
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Initials { get; set; } = "?";
    public string AvatarUrl { get; set; } = string.Empty;
}
