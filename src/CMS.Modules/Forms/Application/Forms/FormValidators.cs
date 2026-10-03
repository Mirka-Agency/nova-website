using FluentValidation;
using CMS.Modules.Forms.Application.AntiSpam;
using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Application.Forms;

public sealed class SaveFormCommandValidator : AbstractValidator<SaveFormCommand>
{
    public SaveFormCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Key)
            .MaximumLength(100)
            .Matches(@"^[A-Za-z0-9_-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.Key))
            .WithMessage("کلید فرم فقط می‌تواند شامل حرف، عدد، خط زیر یا خط تیره باشد.");
        RuleFor(x => x.Slug).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Slug));
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.SuccessMessage).MaximumLength(1000);
        RuleFor(x => x.RedirectUrl).MaximumLength(1000);
        RuleFor(x => x.SubmitButtonText).MaximumLength(100);
        RuleFor(x => x.NotifyEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256)
            .When(x => x.SendEmailNotification);
        RuleFor(x => x.NotifyEmailSubject).MaximumLength(300);
        RuleFor(x => x.NotifySenderName).MaximumLength(200);
        RuleFor(x => x.NotifyReplyToFieldKey).MaximumLength(100);
        RuleFor(x => x.AutoReplySubject)
            .NotEmpty()
            .MaximumLength(300)
            .When(x => x.AutoReplyEnabled);
        RuleFor(x => x.AutoReplyBody)
            .NotEmpty()
            .MaximumLength(4000)
            .When(x => x.AutoReplyEnabled);
        RuleFor(x => x.AutoReplyEmailFieldKey)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => x.AutoReplyEnabled && !x.AutoReplyEmailFieldId.HasValue);
        RuleFor(x => x.SubmitBehaviorType)
            .Must(t => string.IsNullOrWhiteSpace(t)
                       || t is "message" or "redirect" or "page")
            .WithMessage("نوع رفتار پس از ارسال نامعتبر است.");
        RuleFor(x => x.RedirectUrl)
            .NotEmpty()
            .When(x => string.Equals(x.SubmitBehaviorType, "redirect", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(x.SubmitBehaviorType, "page", StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.RedirectUrl)
            .Must(u => string.IsNullOrWhiteSpace(u)
                       || u.Trim().StartsWith('/')
                       || Uri.TryCreate(u, UriKind.Absolute, out var abs) && abs.Scheme is "http" or "https")
            .When(x => !string.IsNullOrWhiteSpace(x.RedirectUrl))
            .WithMessage("آدرس هدایت باید مسیر نسبی (/...) یا http(s) باشد.");
        RuleFor(x => x.WebhookUrl)
            .NotEmpty()
            .MaximumLength(2000)
            .When(x => x.WebhookEnabled);
        RuleFor(x => x.WebhookSecret).MaximumLength(500);
        RuleFor(x => x.AntiSpamProvider)
            .Must(p => string.IsNullOrWhiteSpace(p)
                       || FormAntiSpamProviderIds.All.Contains(p.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("ارائه‌دهنده ضداسپم نامعتبر است.");
        RuleFor(x => x.AntiSpamSiteKey).MaximumLength(500);
        RuleFor(x => x.AntiSpamSecretKey).MaximumLength(500);
        RuleFor(x => x.SimpleCaptchaExpected).MaximumLength(200);
    }
}

public sealed class SaveFormFieldCommandValidator : AbstractValidator<SaveFormFieldCommand>
{
    public SaveFormFieldCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100)
            .Matches(@"^[A-Za-z0-9_-]+$")
            .WithMessage("کلید فیلد فقط می‌تواند شامل حرف لاتین، عدد، خط زیر یا خط تیره باشد.");
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Placeholder).MaximumLength(300);
        RuleFor(x => x.HelpText).MaximumLength(1000);
        RuleFor(x => x.SettingsJson).MaximumLength(4000);
        RuleFor(x => x.OptionsCsv)
            .NotEmpty()
            .When(x => x.FieldType is FormFieldType.Select or FormFieldType.Radio or FormFieldType.CheckboxGroup)
            .WithMessage("برای فیلد انتخابی، حداقل یک گزینه لازم است.");
        RuleFor(x => x.Pattern).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Pattern));
        RuleFor(x => x.LayoutWidth)
            .Must(w => w is null or "full" or "half")
            .WithMessage("عرض چیدمان باید full یا half باشد.");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
