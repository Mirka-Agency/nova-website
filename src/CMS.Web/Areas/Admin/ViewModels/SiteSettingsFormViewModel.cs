using System.ComponentModel.DataAnnotations;

namespace CMS.Web.Areas.Admin.ViewModels;

public sealed class SiteSettingsFormViewModel
{
    [Required(ErrorMessage = "نام سایت الزامی است.")]
    [StringLength(200, ErrorMessage = "نام سایت حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string SiteName { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "شعار حداکثر ۳۰۰ نویسه می‌تواند باشد.")]
    public string? Tagline { get; set; }

    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    [StringLength(256, ErrorMessage = "ایمیل حداکثر ۲۵۶ نویسه می‌تواند باشد.")]
    public string? ContactEmail { get; set; }

    [StringLength(40, ErrorMessage = "تلفن حداکثر ۴۰ نویسه می‌تواند باشد.")]
    public string? ContactPhone { get; set; }

    [StringLength(1000, ErrorMessage = "آدرس خیلی طولانی است.")]
    public string? Address { get; set; }

    [StringLength(200, ErrorMessage = "ساعات کاری حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string? BusinessHours { get; set; }

    [StringLength(500, ErrorMessage = "متن پاورقی خیلی طولانی است.")]
    public string? FooterText { get; set; }

    public string? PrivacyHtml { get; set; }

    [StringLength(2000, ErrorMessage = "آدرس لوگو خیلی طولانی است.")]
    public string? LogoUrl { get; set; }

    [StringLength(2000, ErrorMessage = "آدرس فاویکون خیلی طولانی است.")]
    public string? FaviconUrl { get; set; }

    [StringLength(200, ErrorMessage = "عنوان متا حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string? MetaTitle { get; set; }

    [StringLength(500, ErrorMessage = "توضیح متا حداکثر ۵۰۰ نویسه می‌تواند باشد.")]
    public string? MetaDescription { get; set; }

    [StringLength(2000, ErrorMessage = "آدرس تصویر OG خیلی طولانی است.")]
    public string? DefaultOgImageUrl { get; set; }

    [StringLength(500, ErrorMessage = "آدرس اینستاگرام خیلی طولانی است.")]
    public string? InstagramUrl { get; set; }

    [StringLength(500, ErrorMessage = "آدرس تلگرام خیلی طولانی است.")]
    public string? TelegramUrl { get; set; }

    [StringLength(500, ErrorMessage = "آدرس اکس خیلی طولانی است.")]
    public string? TwitterUrl { get; set; }

    [StringLength(500, ErrorMessage = "آدرس لینکدین خیلی طولانی است.")]
    public string? LinkedInUrl { get; set; }

    [StringLength(500, ErrorMessage = "آدرس آپارات خیلی طولانی است.")]
    public string? AparatUrl { get; set; }

    [StringLength(500, ErrorMessage = "آدرس فیسبوک خیلی طولانی است.")]
    public string? FacebookUrl { get; set; }

    [StringLength(500, ErrorMessage = "آدرس یوتیوب خیلی طولانی است.")]
    public string? YouTubeUrl { get; set; }

    [StringLength(500, ErrorMessage = "آدرس واتس‌اپ خیلی طولانی است.")]
    public string? WhatsAppUrl { get; set; }

    public bool MaintenanceMode { get; set; }

    [StringLength(500, ErrorMessage = "پیام تعمیرات خیلی طولانی است.")]
    public string? MaintenanceMessage { get; set; }
}

public sealed class SiteScriptsFormViewModel
{
    [StringLength(16000, ErrorMessage = "اسکریپت‌های head خیلی طولانی است.")]
    public string? HeadScripts { get; set; }

    [StringLength(16000, ErrorMessage = "اسکریپت‌های ابتدای body خیلی طولانی است.")]
    public string? BodyOpenScripts { get; set; }

    [StringLength(16000, ErrorMessage = "اسکریپت‌های انتهای body خیلی طولانی است.")]
    public string? BodyCloseScripts { get; set; }
}
