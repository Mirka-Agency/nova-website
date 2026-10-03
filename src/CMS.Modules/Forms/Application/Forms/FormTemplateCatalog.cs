using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Application.Forms;

public static class FormTemplateCatalog
{
    public static IReadOnlyList<FormTemplateInfoDto> List() =>
    [
        new(FormTemplateKind.Contact, "فرم تماس", "نام، ایمیل، تلفن و پیام"),
        new(FormTemplateKind.Consultation, "درخواست مشاوره", "اطلاعات تماس + موضوع و جزئیات"),
        new(FormTemplateKind.Hiring, "استخدام", "اطلاعات متقاضی + رزومه (آپلود فایل)")
    ];

    public static (string Name, string Slug, string Description, IReadOnlyList<(string Key, string Label, FormFieldType Type, bool Required, string? Options, int Order)> Fields)
        Get(FormTemplateKind kind) => kind switch
    {
        FormTemplateKind.Contact => (
            "فرم تماس",
            "contact",
            "از طریق این فرم با ما در ارتباط باشید.",
            [
                ("full_name", "نام و نام خانوادگی", FormFieldType.Text, true, null, 1),
                ("email", "ایمیل", FormFieldType.Email, true, null, 2),
                ("phone", "تلفن", FormFieldType.Phone, false, null, 3),
                ("message", "پیام", FormFieldType.TextArea, true, null, 4)
            ]),
        FormTemplateKind.Consultation => (
            "درخواست مشاوره",
            "consultation",
            "برای دریافت مشاوره فرم را تکمیل کنید.",
            [
                ("full_name", "نام و نام خانوادگی", FormFieldType.Text, true, null, 1),
                ("email", "ایمیل", FormFieldType.Email, true, null, 2),
                ("phone", "تلفن", FormFieldType.Phone, true, null, 3),
                ("topic", "موضوع مشاوره", FormFieldType.Select, true, "فروش|پشتیبانی|فنی|سایر", 4),
                ("details", "توضیحات", FormFieldType.TextArea, true, null, 5)
            ]),
        FormTemplateKind.Hiring => (
            "استخدام",
            "hiring",
            "رزومه و اطلاعات خود را ارسال کنید.",
            [
                ("full_name", "نام و نام خانوادگی", FormFieldType.Text, true, null, 1),
                ("email", "ایمیل", FormFieldType.Email, true, null, 2),
                ("phone", "تلفن", FormFieldType.Phone, true, null, 3),
                ("position", "موقعیت شغلی", FormFieldType.Select, true, "توسعه‌دهنده|طراح|فروش|پشتیبانی|سایر", 4),
                ("resume", "رزومه", FormFieldType.FileUpload, true, null, 5),
                ("cover_letter", "متن معرفی", FormFieldType.TextArea, false, null, 6)
            ]),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
