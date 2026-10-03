using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Application.Forms;

/// <summary>
/// Code-defined system forms. Add definitions here; <c>FormSystemSeeder</c> creates them on startup.
/// Keys and system field contracts are protected in admin (readonly essentials).
/// </summary>
public static class SystemFormCatalog
{
    /// <summary>All system forms to ensure on startup.</summary>
    public static IReadOnlyList<SystemFormDefinition> All { get; } =
    [
        Contact(),
        Booking()
    ];

    public static SystemFormDefinition Contact() => new(
        Key: "contact",
        Slug: "contact",
        Name: "فرم تماس",
        Description: "برای مشاوره، نوبت یا سوالات اداری پیام بگذارید.",
        Publish: true,
        SuccessMessage: "پیام شما دریافت شد. به‌زودی با شما تماس می‌گیریم.",
        SubmitButtonText: "ارسال پیام",
        Fields:
        [
            new SystemFormField(
                "full_name",
                "نام و نام خانوادگی",
                FormFieldType.Text,
                Required: true,
                Order: 1,
                Placeholder: "مثلاً مریم احمدی"),
            new SystemFormField(
                "phone",
                "شماره تماس",
                FormFieldType.Phone,
                Required: true,
                Order: 2,
                Placeholder: "۰۹۱۲xxxxxxx"),
            new SystemFormField(
                "email",
                "ایمیل",
                FormFieldType.Email,
                Required: false,
                Order: 3,
                Placeholder: "name@example.com",
                HelpText: "اختیاری"),
            new SystemFormField(
                "subject",
                "موضوع",
                FormFieldType.Select,
                Required: true,
                Order: 4,
                OptionsCsv: "appointment:رزرو نوبت / مشاوره|thyroid:سوال درباره تیروئید|parathyroid:سوال درباره پاراتیروئید|adrenal:سوال درباره فوق کلیه|admin:پیگیری اداری|other:سایر موارد"),
            new SystemFormField(
                "message",
                "پیام شما",
                FormFieldType.TextArea,
                Required: true,
                Order: 5,
                Placeholder: "پیام خود را بنویسید...")
        ]);

    public static SystemFormDefinition Booking() => new(
        Key: "booking",
        Slug: "booking",
        Name: "رزرو نوبت مشاوره",
        Description: "نام و شماره تماس خود را وارد کنید تا برای هماهنگی نوبت با شما تماس بگیریم.",
        Publish: true,
        SuccessMessage: "درخواست شما ثبت شد. به‌زودی برای هماهنگی نوبت با شما تماس می‌گیریم.",
        SubmitButtonText: "ثبت درخواست نوبت",
        Fields:
        [
            new SystemFormField(
                "full_name",
                "نام و نام خانوادگی",
                FormFieldType.Text,
                Required: true,
                Order: 1,
                Placeholder: "مثلاً مریم احمدی"),
            new SystemFormField(
                "phone",
                "شماره تماس",
                FormFieldType.Phone,
                Required: true,
                Order: 2,
                Placeholder: "۰۹۱۲xxxxxxx"),
            new SystemFormField(
                "service",
                "موضوع مراجعه",
                FormFieldType.Select,
                Required: false,
                Order: 3,
                OptionsCsv: "thyroid:مشاوره تخصصی تیروئید|parathyroid:مشاوره پاراتیروئید|adrenal:مشاوره فوق کلیه (آدرنال)|pancreas:مشاوره پانکراس|toetva:جراحی تیروئید بدون جای زخم (TOETVA)|other:سایر خدمات غدد",
                HelpText: "اختیاری"),
            new SystemFormField(
                "message",
                "توضیح کوتاه",
                FormFieldType.TextArea,
                Required: false,
                Order: 4,
                Placeholder: "در صورت نیاز توضیح دهید...",
                HelpText: "اختیاری")
        ]);
}

public sealed record SystemFormDefinition(
    string Key,
    string Slug,
    string Name,
    string? Description,
    IReadOnlyList<SystemFormField> Fields,
    bool Publish = true,
    string? SuccessMessage = null,
    string? SubmitButtonText = null);

public sealed record SystemFormField(
    string Key,
    string Label,
    FormFieldType Type,
    bool Required,
    int Order,
    string? OptionsCsv = null,
    string? Placeholder = null,
    string? HelpText = null);
