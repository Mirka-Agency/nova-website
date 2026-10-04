using CMS.Application.Settings;
using FluentValidation;

namespace CMS.Application;

public sealed class UpdateSiteSettingsCommandValidator : AbstractValidator<UpdateSiteSettingsCommand>
{
    public UpdateSiteSettingsCommandValidator()
    {
        RuleFor(x => x.SiteName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Tagline).MaximumLength(300);
        RuleFor(x => x.ContactEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ContactEmail)).MaximumLength(256);
        RuleFor(x => x.ContactPhone).MaximumLength(40);
        RuleFor(x => x.Address).MaximumLength(1000);
        RuleFor(x => x.BusinessHours).MaximumLength(200);
        RuleFor(x => x.FooterText).MaximumLength(500);
        RuleFor(x => x.PrivacyHtml).MaximumLength(100_000);

        RuleFor(x => x.LogoUrl).MaximumLength(2000).Must(BeHttpOrSiteUrl).When(x => !string.IsNullOrWhiteSpace(x.LogoUrl))
            .WithMessage("آدرس لوگو باید http(s) یا مسیر نسبی سایت باشد.");
        RuleFor(x => x.FaviconUrl).MaximumLength(2000).Must(BeHttpOrSiteUrl).When(x => !string.IsNullOrWhiteSpace(x.FaviconUrl))
            .WithMessage("آدرس فاویکون باید http(s) یا مسیر نسبی سایت باشد.");
        RuleFor(x => x.DefaultOgImageUrl).MaximumLength(2000).Must(BeHttpOrSiteUrl).When(x => !string.IsNullOrWhiteSpace(x.DefaultOgImageUrl))
            .WithMessage("آدرس تصویر OG باید http(s) یا مسیر نسبی سایت باشد.");

        RuleFor(x => x.MetaTitle).MaximumLength(200);
        RuleFor(x => x.MetaDescription).MaximumLength(500);

        RuleFor(x => x.InstagramUrl).MaximumLength(500).Must(BeAbsoluteHttpUrl).When(x => !string.IsNullOrWhiteSpace(x.InstagramUrl));
        RuleFor(x => x.TelegramUrl).MaximumLength(500).Must(BeAbsoluteHttpUrl).When(x => !string.IsNullOrWhiteSpace(x.TelegramUrl));
        RuleFor(x => x.TwitterUrl).MaximumLength(500).Must(BeAbsoluteHttpUrl).When(x => !string.IsNullOrWhiteSpace(x.TwitterUrl));
        RuleFor(x => x.LinkedInUrl).MaximumLength(500).Must(BeAbsoluteHttpUrl).When(x => !string.IsNullOrWhiteSpace(x.LinkedInUrl));
        RuleFor(x => x.AparatUrl).MaximumLength(500).Must(BeAbsoluteHttpUrl).When(x => !string.IsNullOrWhiteSpace(x.AparatUrl));
        RuleFor(x => x.FacebookUrl).MaximumLength(500).Must(BeAbsoluteHttpUrl).When(x => !string.IsNullOrWhiteSpace(x.FacebookUrl));
        RuleFor(x => x.YouTubeUrl).MaximumLength(500).Must(BeAbsoluteHttpUrl).When(x => !string.IsNullOrWhiteSpace(x.YouTubeUrl));
        RuleFor(x => x.WhatsAppUrl).MaximumLength(500).Must(BeAbsoluteHttpUrl).When(x => !string.IsNullOrWhiteSpace(x.WhatsAppUrl));

        RuleFor(x => x.MaintenanceMessage).MaximumLength(500);
    }

    private static bool BeAbsoluteHttpUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool BeHttpOrSiteUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        var trimmed = value.Trim();
        if (trimmed.StartsWith('/') && !trimmed.StartsWith("//", StringComparison.Ordinal))
            return trimmed.Length <= 2000;
        return BeAbsoluteHttpUrl(trimmed);
    }
}

public sealed class UpdateSiteScriptsCommandValidator : AbstractValidator<UpdateSiteScriptsCommand>
{
    public UpdateSiteScriptsCommandValidator()
    {
        RuleFor(x => x.HeadScripts).MaximumLength(16000);
        RuleFor(x => x.BodyOpenScripts).MaximumLength(16000);
        RuleFor(x => x.BodyCloseScripts).MaximumLength(16000);
    }
}
