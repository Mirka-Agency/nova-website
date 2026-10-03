using System.ComponentModel.DataAnnotations;

namespace CMS.Web.ViewModels;

public sealed class CustomerLoginViewModel
{
    public string Step { get; set; } = "phone";

    [Display(Name = "موبایل")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "کد تأیید")]
    public string? Code { get; set; }

    public string? ReturnUrl { get; set; }
    public int? RetryAfterSeconds { get; set; }
}

public sealed class CustomerProfileFormViewModel
{
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "نام و نام خانوادگی الزامی است.")]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "ایمیل نامعتبر است.")]
    [StringLength(256)]
    public string? Email { get; set; }
}

public sealed class CustomerAddressFormViewModel
{
    [Required(ErrorMessage = "نام تحویل‌گیرنده الزامی است.")]
    [StringLength(200)]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "موبایل تحویل‌گیرنده الزامی است.")]
    [StringLength(40)]
    public string RecipientPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "استان الزامی است.")]
    [StringLength(100)]
    public string Province { get; set; } = string.Empty;

    [Required(ErrorMessage = "شهر الزامی است.")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [Required(ErrorMessage = "آدرس الزامی است.")]
    [StringLength(1000)]
    public string AddressLine { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}
