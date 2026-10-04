using FluentValidation;
using CMS.Modules.Seo.Domain.Enums;

namespace CMS.Modules.Seo.Application.Settings;

public sealed class UpdateSeoSiteSettingsCommandValidator : AbstractValidator<UpdateSeoSiteSettingsCommand>
{
    public UpdateSeoSiteSettingsCommandValidator()
    {
        RuleFor(x => x.OrganizationName).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.OrganizationName));
        RuleFor(x => x.OrganizationUrl).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.OrganizationUrl));
        RuleFor(x => x.OrganizationLogoUrl).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.OrganizationLogoUrl));
        RuleFor(x => x.DefaultSchemaType)
            .MaximumLength(64)
            .Must(v => string.IsNullOrWhiteSpace(v) || SeoSchemaTypes.IsKnown(v))
            .WithMessage("نوع اسکیمای پیش‌فرض نامعتبر است.")
            .When(x => !string.IsNullOrWhiteSpace(x.DefaultSchemaType));
        RuleFor(x => x.RobotsTxt).MaximumLength(8000).When(x => !string.IsNullOrWhiteSpace(x.RobotsTxt));
        RuleFor(x => x.TwitterSiteHandle).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.TwitterSiteHandle));
    }
}
