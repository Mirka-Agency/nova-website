using System.Globalization;
using FluentValidation;

namespace CMS.Application.Validation;

/// <summary>Configures FluentValidation messages and property names for Farsi UI.</summary>
public static class FluentValidationPersian
{
    private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Title"] = "عنوان",
        ["Name"] = "نام",
        ["Slug"] = "نامک",
        ["Description"] = "توضیحات",
        ["Body"] = "متن",
        ["Price"] = "قیمت",
        ["SalePrice"] = "قیمت فروش",
        ["Sku"] = "کد SKU",
        ["AttributeSummary"] = "خلاصه ویژگی‌ها",
        ["StockQuantity"] = "موجودی",
        ["UnlimitedStock"] = "همیشه موجود",
        ["Weight"] = "وزن",
        ["Status"] = "وضعیت",
        ["Currency"] = "واحد پول",
        ["Key"] = "کلید",
        ["Label"] = "برچسب",
        ["Options"] = "گزینه‌ها",
        ["Email"] = "ایمیل",
        ["NotifyEmail"] = "ایمیل اعلان",
        ["CustomerName"] = "نام مشتری",
        ["CustomerEmail"] = "ایمیل مشتری",
        ["CustomerPhone"] = "تلفن مشتری",
        ["ShippingAddress"] = "آدرس ارسال",
        ["Notes"] = "یادداشت",
        ["FileName"] = "نام فایل",
        ["ContentType"] = "نوع محتوا",
        ["Content"] = "محتوا",
        ["SizeBytes"] = "حجم فایل",
        ["CoverImageUrl"] = "تصویر شاخص",
        ["CoverImageAlt"] = "متن جایگزین تصویر شاخص",
        ["ImageUrl"] = "آدرس تصویر",
        ["SiteName"] = "نام سایت",
        ["Tagline"] = "شعار",
        ["ContactEmail"] = "ایمیل تماس",
        ["ContactPhone"] = "تلفن تماس",
        ["Address"] = "آدرس",
        ["FooterText"] = "متن پاورقی",
        ["LogoUrl"] = "آدرس لوگو",
        ["FaviconUrl"] = "آدرس فاویکون",
        ["DefaultOgImageUrl"] = "تصویر OG",
        ["MetaTitle"] = "عنوان متا",
        ["MetaDescription"] = "توضیح متا",
        ["SeoKeywords"] = "کلمات کلیدی سئو",
        ["CanonicalUrl"] = "آدرس کنونیکال",
        ["OgTitle"] = "عنوان Open Graph",
        ["OgDescription"] = "توضیحات Open Graph",
        ["OgImageUrl"] = "تصویر Open Graph",
        ["Excerpt"] = "توضیحات کوتاه",
        ["AuthorUserId"] = "نویسنده",
        ["PublishedAtUtc"] = "تاریخ انتشار",
        ["InstagramUrl"] = "اینستاگرام",
        ["TelegramUrl"] = "تلگرام",
        ["TwitterUrl"] = "اکس",
        ["LinkedInUrl"] = "لینکدین",
        ["AparatUrl"] = "آپارات",
        ["FacebookUrl"] = "فیسبوک",
        ["YouTubeUrl"] = "یوتیوب",
        ["WhatsAppUrl"] = "واتس‌اپ",
        ["HeadScripts"] = "اسکریپت‌های head",
        ["BodyOpenScripts"] = "اسکریپت‌های بعد از body",
        ["BodyCloseScripts"] = "اسکریپت‌های انتهای body",
        ["MaintenanceMessage"] = "پیام تعمیرات",
        ["Mode"] = "حالت فروشگاه",
        ["Quantity"] = "تعداد",
        ["FullName"] = "نام کامل",
        ["PhoneNumber"] = "شماره موبایل",
        ["Password"] = "رمز عبور",
        ["NewPassword"] = "رمز عبور جدید",
        ["CurrentPassword"] = "رمز عبور فعلی",
        ["SortOrder"] = "ترتیب نمایش",
        ["OptionsCsv"] = "گزینه‌ها",
        ["FieldType"] = "نوع فیلد",
        ["CategoryId"] = "دسته",
        ["File"] = "فایل",
        ["CoverImage"] = "تصویر شاخص",
        ["Image"] = "تصویر",
        ["Website"] = "وب‌سایت",
        ["IsAvailable"] = "موجودی",
        ["IsPurchasable"] = "قابل خرید",
        ["Publish"] = "انتشار",
        ["SendEmailNotification"] = "اعلان ایمیل",
    };

    public static void Configure()
    {
        var culture = new CultureInfo("fa");
        ValidatorOptions.Global.LanguageManager.Culture = culture;
        ValidatorOptions.Global.DisplayNameResolver = (_, member, _) =>
        {
            if (member is null)
                return null;

            return DisplayNames.TryGetValue(member.Name, out var fa) ? fa : member.Name;
        };
    }
}
