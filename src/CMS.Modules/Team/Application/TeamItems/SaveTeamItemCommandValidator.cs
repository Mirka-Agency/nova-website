using FluentValidation;

namespace CMS.Modules.Team.Application.TeamItems;

public sealed class SaveTeamItemCommandValidator : AbstractValidator<SaveTeamItemCommand>
{
    public SaveTeamItemCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(300)
            .When(x => x.Publish);

        RuleFor(x => x.Title)
            .MaximumLength(300)
            .When(x => !x.Publish);

        RuleFor(x => x.Subtitle)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.Subtitle));

        RuleFor(x => x.Body)
            .NotNull()
            .MaximumLength(500_000);

        RuleFor(x => x.Slug)
            .MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.Excerpt)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrWhiteSpace(x.Excerpt));

        RuleFor(x => x.Highlights)
            .MaximumLength(TeamHighlights.MaxTextLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Highlights));

        RuleFor(x => x.SpecialtyPathJson)
            .MaximumLength(TeamSpecialtyPathJson.MaxJsonLength)
            .Must(value => TeamSpecialtyPathJson.TryValidate(value, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.SpecialtyPathJson))
            .WithMessage(x =>
            {
                TeamSpecialtyPathJson.TryValidate(x.SpecialtyPathJson, out var error);
                return error ?? "حوزه فعالیت نامعتبر است.";
            });

        RuleFor(x => x.EducationPathJson)
            .MaximumLength(TeamEducationPathJson.MaxJsonLength)
            .Must(value => TeamEducationPathJson.TryValidate(value, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.EducationPathJson))
            .WithMessage(x =>
            {
                TeamEducationPathJson.TryValidate(x.EducationPathJson, out var error);
                return error ?? "مسیر تخصصی نامعتبر است.";
            });

        RuleFor(x => x.FaqJson)
            .MaximumLength(TeamFaqJson.MaxJsonLength)
            .Must(value => TeamFaqJson.TryValidate(value, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.FaqJson))
            .WithMessage(x =>
            {
                TeamFaqJson.TryValidate(x.FaqJson, out var error);
                return error ?? "سوالات متداول نامعتبر است.";
            });

        RuleFor(x => x.CoverImageUrl)
            .MaximumLength(1000)
            .Must(BeValidUrlOrPath)
            .When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl))
            .WithMessage("تصویر شاخص باید آدرس مطلق یا مسیر نسبی سایت باشد.");

        RuleFor(x => x.AvatarImageUrl)
            .MaximumLength(1000)
            .Must(BeValidUrlOrPath)
            .When(x => !string.IsNullOrWhiteSpace(x.AvatarImageUrl))
            .WithMessage("تصویر آواتار باید آدرس مطلق یا مسیر نسبی سایت باشد.");

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
