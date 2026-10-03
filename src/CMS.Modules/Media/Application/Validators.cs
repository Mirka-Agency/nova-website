using CMS.Application.Storage;
using CMS.Modules.Media.Application.Assets;
using CMS.Modules.Media.Application.Imaging;
using FluentValidation;

namespace CMS.Modules.Media.Application;

public sealed class UploadMediaCommandValidator : AbstractValidator<UploadMediaCommand>
{
    public UploadMediaCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(128)
            .Must(ImageUploadRules.IsAllowedContentType)
            .WithMessage("فقط تصاویر JPEG، PNG، WebP یا GIF مجاز هستند.");
        RuleFor(x => x.SizeBytes)
            .Must(ImageUploadRules.IsWithinSizeLimit)
            .WithMessage("حجم تصویر باید حداکثر ۵ مگابایت باشد.");
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.Optimize!).SetValidator(new MediaOptimizeOptionsValidator())
            .When(x => x.Optimize is not null);
    }
}

public sealed class ReplaceMediaFileCommandValidator : AbstractValidator<ReplaceMediaFileCommand>
{
    public ReplaceMediaFileCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(260);
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(128)
            .Must(ImageUploadRules.IsAllowedContentType)
            .WithMessage("فقط تصاویر JPEG، PNG، WebP یا GIF مجاز هستند.");
        RuleFor(x => x.SizeBytes)
            .Must(ImageUploadRules.IsWithinSizeLimit)
            .WithMessage("حجم تصویر باید حداکثر ۵ مگابایت باشد.");
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.Optimize!).SetValidator(new MediaOptimizeOptionsValidator())
            .When(x => x.Optimize is not null);
    }
}

public sealed class MediaOptimizeOptionsValidator : AbstractValidator<MediaOptimizeOptions>
{
    public MediaOptimizeOptionsValidator()
    {
        RuleFor(x => x.MaxDisplayDimension)
            .InclusiveBetween(MediaOptimizeOptions.MinDimension, MediaOptimizeOptions.MaxDimension)
            .When(x => x.MaxDisplayDimension.HasValue)
            .WithMessage($"حداکثر بعد تصویر باید بین {MediaOptimizeOptions.MinDimension} تا {MediaOptimizeOptions.MaxDimension} باشد.");

        RuleFor(x => x.Quality)
            .InclusiveBetween(MediaOptimizeOptions.MinQuality, MediaOptimizeOptions.MaxQuality)
            .When(x => x.Quality.HasValue)
            .WithMessage($"کیفیت فشرده‌سازی باید بین {MediaOptimizeOptions.MinQuality} تا {MediaOptimizeOptions.MaxQuality} باشد.");
    }
}
