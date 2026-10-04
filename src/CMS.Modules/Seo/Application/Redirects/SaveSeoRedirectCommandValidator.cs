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
            .Must(c => c is 200 or 301 or 302)
            .WithMessage("کد وضعیت باید ۲۰۰، ۳۰۱ یا ۳۰۲ باشد.");

        RuleFor(x => x.ToUrl)
            .Must(url => !string.IsNullOrWhiteSpace(url)
                         && !Uri.TryCreate(url.Trim(), UriKind.Absolute, out _)
                         && url.Trim().Split('?', 2)[0].TrimStart().StartsWith('/'))
            .When(x => x.StatusCode == 200)
            .WithMessage("برای بازنویسی ۲۰۰ مقصد باید مسیر داخلی باشد (مثلاً /page).");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}
