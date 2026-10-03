using CMS.Modules.Popup.Domain.Enums;
using FluentValidation;

namespace CMS.Modules.Popup.Application.Popups;

public sealed class SavePopupCommandValidator : AbstractValidator<SavePopupCommand>
{
    public SavePopupCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Slug));
        RuleFor(x => x.BodyText).MaximumLength(20_000);
        RuleFor(x => x.ImageUrl).MaximumLength(1000);
        RuleFor(x => x.ContentHtml).MaximumLength(200_000);
        RuleFor(x => x.TriggerType).NotEmpty().MaximumLength(64);
        RuleFor(x => x.TriggerSelector).MaximumLength(300);
        RuleFor(x => x.TriggerConfigJson).MaximumLength(20_000);
        RuleFor(x => x.ExtensionSettingsJson).MaximumLength(50_000);
        RuleFor(x => x.PagePaths).MaximumLength(8_000);
        RuleFor(x => x.SortOrder).InclusiveBetween(-1000, 10000);
        RuleFor(x => x.CtaText).MaximumLength(120);
        RuleFor(x => x.CtaAction).MaximumLength(32);
        RuleFor(x => x.CtaUrl).MaximumLength(1000);

        RuleFor(x => x.TriggerDelaySeconds)
            .InclusiveBetween(0, 3600)
            .When(x => string.Equals(x.TriggerType, PopupTriggerTypes.Timer, StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.TriggerScrollPercent)
            .InclusiveBetween(1, 100)
            .When(x => string.Equals(x.TriggerType, PopupTriggerTypes.Scroll, StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.PagePaths)
            .NotEmpty()
            .When(x => x.PageTargetMode is PopupPageTargetMode.Include or PopupPageTargetMode.Exclude)
            .WithMessage("حداقل یک مسیر صفحه وارد کنید.");

        RuleFor(x => x.CtaText)
            .NotEmpty()
            .When(x => PopupCtaActions.Normalize(x.CtaAction) != PopupCtaActions.None)
            .WithMessage("متن دکمه الزامی است.");

        RuleFor(x => x.CtaUrl)
            .NotEmpty()
            .When(x => PopupCtaActions.Normalize(x.CtaAction) == PopupCtaActions.Url)
            .WithMessage("آدرس لینک دکمه الزامی است.");

        RuleFor(x => x.CtaTargetPopupId)
            .NotNull()
            .When(x => PopupCtaActions.Normalize(x.CtaAction) == PopupCtaActions.OpenPopup)
            .WithMessage("پاپ‌آپ مقصد را انتخاب کنید.");
    }
}
