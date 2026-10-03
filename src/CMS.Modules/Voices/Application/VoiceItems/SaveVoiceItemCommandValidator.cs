using FluentValidation;

namespace CMS.Modules.Voices.Application.VoiceItems;

public sealed class SaveVoiceItemCommandValidator : AbstractValidator<SaveVoiceItemCommand>
{
    public SaveVoiceItemCommandValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200)
            .WithMessage("نام کاربر الزامی است و حداکثر ۲۰۰ نویسه می‌تواند باشد.");
        RuleFor(x => x.Subtitle).MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.Subtitle));
        RuleFor(x => x.Description).MaximumLength(2000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
        RuleFor(x => x.AudioUrl).NotEmpty().MaximumLength(1000)
            .WithMessage("آدرس فایل صوتی الزامی است و حداکثر ۱۰۰۰ نویسه می‌تواند باشد.");
        RuleFor(x => x.SortOrder).InclusiveBetween(-1000, 10000);
    }
}
