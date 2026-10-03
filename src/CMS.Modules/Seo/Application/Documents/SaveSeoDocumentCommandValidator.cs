using FluentValidation;
using CMS.Modules.Seo.Domain.Enums;

namespace CMS.Modules.Seo.Application.Documents;

public sealed class SaveSeoDocumentCommandValidator : AbstractValidator<SaveSeoDocumentCommand>
{
    public SaveSeoDocumentCommandValidator()
    {
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .MaximumLength(64)
            .Must(SeoContentTypes.IsKnown)
            .WithMessage("نوع محتوا نامعتبر است.");

        RuleFor(x => x.ContentId)
            .NotEmpty()
            .WithMessage("شناسه محتوا الزامی است.");

        RuleFor(x => x.FocusKeyword)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.FocusKeyword));

        RuleFor(x => x.SchemaType)
            .MaximumLength(64)
            .Must(v => string.IsNullOrWhiteSpace(v) || SeoSchemaTypes.IsKnown(v))
            .WithMessage("نوع اسکیما نامعتبر است.")
            .When(x => !string.IsNullOrWhiteSpace(x.SchemaType));

        RuleFor(x => x.SeoScore)
            .InclusiveBetween(0, 100)
            .When(x => x.SeoScore.HasValue);
    }
}
