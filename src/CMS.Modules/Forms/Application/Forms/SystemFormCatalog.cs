using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Application.Forms;

/// <summary>
/// Code-defined system forms. Add definitions here; <c>FormSystemSeeder</c> creates them on startup.
/// Keys and system field contracts are protected in admin (readonly essentials).
/// </summary>
public static class SystemFormCatalog
{
    /// <summary>All system forms to ensure on startup. Edit this list for your site.</summary>
    public static IReadOnlyList<SystemFormDefinition> All { get; } =
    [
        // Example — remove or rename as needed:
        // Contact(),
    ];

    /// <summary>Sample contact form definition (not registered until added to <see cref="All"/>).</summary>
    public static SystemFormDefinition Contact() => new(
        Key: "contact",
        Slug: "contact",
        Name: "فرم تماس",
        Description: "از طریق این فرم با ما در ارتباط باشید.",
        Publish: true,
        Fields:
        [
            new SystemFormField("full_name", "نام و نام خانوادگی", FormFieldType.Text, Required: true, Order: 1),
            new SystemFormField("email", "ایمیل", FormFieldType.Email, Required: true, Order: 2),
            new SystemFormField("phone", "تلفن", FormFieldType.Phone, Required: false, Order: 3),
            new SystemFormField("message", "پیام", FormFieldType.TextArea, Required: true, Order: 4)
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
