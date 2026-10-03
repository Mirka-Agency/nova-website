using FluentValidation;
using CMS.Modules.Services.Application.Categories;

namespace CMS.Modules.Services.Application.Categories;

public sealed class SaveCategoryCommandValidator : AbstractValidator<SaveCategoryCommand>
{
    public SaveCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Slug));
        RuleFor(x => x.Description).MaximumLength(100_000);
        RuleFor(x => x.ImageUrl)
            .MaximumLength(1000)
            .Must(BeValidUrlOrPath)
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
            .WithMessage("آدرس تصویر باید مطلق یا مسیر نسبی سایت باشد.");
    }

    private static bool BeValidUrlOrPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var trimmed = value.Trim();
        if (trimmed.StartsWith('/'))
            return trimmed.Length > 1;

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
