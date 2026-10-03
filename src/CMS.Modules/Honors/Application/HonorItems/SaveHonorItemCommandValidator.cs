using FluentValidation;

namespace CMS.Modules.Honors.Application.HonorItems;

public sealed class SaveHonorItemCommandValidator : AbstractValidator<SaveHonorItemCommand>
{
    public SaveHonorItemCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200)
            .WithMessage("عنوان الزامی است و حداکثر ۲۰۰ نویسه می‌تواند باشد.");
        RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(1000)
            .WithMessage("آدرس تصویر الزامی است و حداکثر ۱۰۰۰ نویسه می‌تواند باشد.");
        RuleFor(x => x.AltText).MaximumLength(300)
            .When(x => !string.IsNullOrWhiteSpace(x.AltText));
        RuleFor(x => x.SortOrder).InclusiveBetween(-1000, 10000);
    }
}
