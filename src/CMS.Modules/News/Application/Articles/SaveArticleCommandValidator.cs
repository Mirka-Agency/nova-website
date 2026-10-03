using CMS.Modules.News.Domain.Enums;
using FluentValidation;

namespace CMS.Modules.News.Application.Articles;

public sealed class SaveArticleCommandValidator : AbstractValidator<SaveArticleCommand>
{
    public SaveArticleCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(300)
            .When(x => x.Publish);

        RuleFor(x => x.Title)
            .MaximumLength(300)
            .When(x => !x.Publish);

        RuleFor(x => x.Body)
            .NotNull()
            .MaximumLength(500_000);

        RuleFor(x => x.Kind)
            .IsInEnum();

        RuleFor(x => x.Slug)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.Excerpt)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Excerpt));

        RuleFor(x => x.Location)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.Location));

        RuleFor(x => x.EventEndAtUtc)
            .GreaterThanOrEqualTo(x => x.EventStartAtUtc)
            .When(x => x.Kind == ArticleKind.Event && x.EventStartAtUtc.HasValue && x.EventEndAtUtc.HasValue)
            .WithMessage("پایان رویداد نمی‌تواند قبل از شروع باشد.");

        RuleFor(x => x.CoverImageUrl)
            .MaximumLength(1000)
            .Must(BeValidUrlOrPath)
            .When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl))
            .WithMessage("تصویر شاخص باید آدرس مطلق یا مسیر نسبی سایت باشد.");

        RuleFor(x => x.GalleryJson)
            .MaximumLength(ArticleGalleryJson.MaxJsonLength)
            .Must(value => ArticleGalleryJson.TryValidate(value, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.GalleryJson))
            .WithMessage(x =>
            {
                ArticleGalleryJson.TryValidate(x.GalleryJson, out var error);
                return error ?? "فرمت گالری تصاویر نامعتبر است.";
            });

        RuleFor(x => x.AttachmentUrl)
            .MaximumLength(1000)
            .Must(BeValidUrlOrPath)
            .When(x => !string.IsNullOrWhiteSpace(x.AttachmentUrl))
            .WithMessage("فایل پیوست باید آدرس مطلق یا مسیر نسبی سایت باشد.");

        RuleFor(x => x.AttachmentFileName)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.AttachmentFileName));

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
