using FluentValidation;

namespace CMS.Modules.Services.Application.ServiceItems;

public sealed class SaveServiceItemCommandValidator : AbstractValidator<SaveServiceItemCommand>
{
    public SaveServiceItemCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(300)
            .When(x => x.Publish == true);

        RuleFor(x => x.Title)
            .MaximumLength(300)
            .When(x => x.Publish != true);

        RuleFor(x => x.Body)
            .NotNull()
            .MaximumLength(500_000);

        RuleFor(x => x.Slug)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.Excerpt)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Excerpt));

        RuleFor(x => x.CoverImageUrl)
            .MaximumLength(1000)
            .Must(BeValidUrlOrPath)
            .When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl))
            .WithMessage("تصویر شاخص باید آدرس مطلق یا مسیر نسبی سایت باشد.");

        RuleFor(x => x.CoverImageAlt)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.CoverImageAlt));

        RuleFor(x => x.AuthorUserId)
            .MaximumLength(450)
            .When(x => !string.IsNullOrWhiteSpace(x.AuthorUserId));

        RuleFor(x => x.AuthorDisplayName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.AuthorDisplayName));

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.MetaTitle));

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.MetaDescription));

        RuleFor(x => x.SeoKeywords)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.SeoKeywords));

        RuleFor(x => x.CanonicalUrl)
            .MaximumLength(1000)
            .Must(BeValidUrlOrPath)
            .When(x => !string.IsNullOrWhiteSpace(x.CanonicalUrl))
            .WithMessage("آدرس کنونیکال باید آدرس مطلق یا مسیر نسبی سایت باشد.");

        RuleFor(x => x.OgTitle)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.OgTitle));

        RuleFor(x => x.OgDescription)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.OgDescription));

        RuleFor(x => x.OgImageUrl)
            .MaximumLength(1000)
            .Must(BeValidUrlOrPath)
            .When(x => !string.IsNullOrWhiteSpace(x.OgImageUrl))
            .WithMessage("تصویر Open Graph باید آدرس مطلق یا مسیر نسبی سایت باشد.");

        RuleFor(x => x.SourcesJson)
            .MaximumLength(ServiceItemSourcesJson.MaxJsonLength)
            .Must(value => ServiceItemSourcesJson.TryValidate(value, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.SourcesJson))
            .WithMessage(x =>
            {
                ServiceItemSourcesJson.TryValidate(x.SourcesJson, out var error);
                return error ?? "منابع علمی نامعتبر است.";
            });

        RuleFor(x => x.FaqJson)
            .MaximumLength(ServiceItemFaqJson.MaxJsonLength)
            .Must(value => ServiceItemFaqJson.TryValidate(value, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.FaqJson))
            .WithMessage(x =>
            {
                ServiceItemFaqJson.TryValidate(x.FaqJson, out var error);
                return error ?? "سوالات متداول نامعتبر است.";
            });

        RuleFor(x => x.ContentGuideJson)
            .MaximumLength(ServiceItemContentGuideJson.MaxJsonLength)
            .Must(value => ServiceItemContentGuideJson.TryValidate(value, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.ContentGuideJson))
            .WithMessage(x =>
            {
                ServiceItemContentGuideJson.TryValidate(x.ContentGuideJson, out var error);
                return error ?? "راهنمای محتوای خدمت نامعتبر است.";
            });

        RuleFor(x => x.VideoSchemaJson)
            .MaximumLength(ServiceItemVideoSchemaJson.MaxJsonLength)
            .Must(value => ServiceItemVideoSchemaJson.TryValidate(value, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.VideoSchemaJson))
            .WithMessage(x =>
            {
                ServiceItemVideoSchemaJson.TryValidate(x.VideoSchemaJson, out var error);
                return error ?? "اسکیمای ویدیو نامعتبر است.";
            });
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
