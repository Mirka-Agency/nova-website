using System.ComponentModel.DataAnnotations;

namespace CMS.Web.Areas.Admin.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "ایمیل الزامی است.")]
    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    [Display(Name = "ایمیل")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز عبور الزامی است.")]
    [DataType(DataType.Password)]
    [Display(Name = "رمز عبور")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "مرا به خاطر بسپار")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }

    /// <summary>Token from Turnstile / reCAPTCHA / hCaptcha widget.</summary>
    public string? CaptchaToken { get; set; }

    public bool CaptchaEnabled { get; set; }

    public string? CaptchaProvider { get; set; }

    public string? CaptchaSiteKey { get; set; }
}
