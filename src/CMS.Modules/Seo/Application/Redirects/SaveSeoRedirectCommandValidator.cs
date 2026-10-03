using FluentValidation;

namespace CMS.Modules.Seo.Application.Redirects;

public sealed class SaveSeoRedirectCommandValidator : AbstractValidator<SaveSeoRedirectCommand>
{
    public SaveSeoRedirectCommandValidator()
    {
        RuleFor(x => x.FromPath)
            .NotEmpty()
            .MaximumLength(500)
            .Must(p => !string.IsNullOrWhiteSpace(p) && (p.Trim().StartsWith('/') || !p.Contains(' ', StringComparison.Ordinal)))
            .WithMessage("مسیر مبدأ باید با / شروع شود.");

        RuleFor(x => x.ToUrl)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.StatusCode)
            .Must(c => c is 301 or 302)
            .WithMessage("کد وضعیت باید ۳۰۱ یا ۳۰۲ باشد.");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
