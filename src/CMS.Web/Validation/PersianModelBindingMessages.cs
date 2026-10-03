using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace CMS.Web.Validation;

/// <summary>Persian ASP.NET Core model-binding error messages (satellite packs are English-only).</summary>
public static class PersianModelBindingMessages
{
    public static void Apply(DefaultModelBindingMessageProvider provider)
    {
        provider.SetValueIsInvalidAccessor(value =>
            string.IsNullOrWhiteSpace(value)
                ? "مقدار واردشده نامعتبر است."
                : $"مقدار «{value}» نامعتبر است.");

        provider.SetAttemptedValueIsInvalidAccessor((value, field) =>
            string.IsNullOrWhiteSpace(value)
                ? $"مقدار «{field}» نامعتبر است."
                : $"مقدار «{value}» برای «{field}» معتبر نیست.");

        provider.SetMissingBindRequiredValueAccessor(field =>
            $"مقدار «{field}» الزامی است.");

        provider.SetValueMustBeANumberAccessor(field =>
            $"«{field}» باید عدد باشد.");

        provider.SetMissingKeyOrValueAccessor(() =>
            "مقدار الزامی است.");

        provider.SetUnknownValueIsInvalidAccessor(field =>
            $"مقدار «{field}» نامعتبر است.");

        provider.SetValueMustNotBeNullAccessor(field =>
            $"مقدار «{field}» الزامی است.");

        provider.SetNonPropertyAttemptedValueIsInvalidAccessor(value =>
            string.IsNullOrWhiteSpace(value)
                ? "مقدار واردشده نامعتبر است."
                : $"مقدار «{value}» نامعتبر است.");

        provider.SetNonPropertyUnknownValueIsInvalidAccessor(() =>
            "مقدار نامعتبر است.");

        provider.SetNonPropertyValueMustBeANumberAccessor(() =>
            "مقدار باید عدد باشد.");
    }
}
