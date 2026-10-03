using CMS.Modules.Comments.Application.Comments;
using FluentValidation;

namespace CMS.Modules.Comments.Application;

public sealed class SubmitCommentCommandValidator : AbstractValidator<SubmitCommentCommand>
{
    public SubmitCommentCommandValidator()
    {
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.TargetTitle).NotEmpty().MaximumLength(300);
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AuthorEmail).MaximumLength(256).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.AuthorEmail));
        RuleFor(x => x.AuthorPhone).MaximumLength(40);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.TargetType).IsInEnum();
    }
}

public sealed class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentCommandValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.PublishedAtUtc).NotEqual(default(DateTime));
    }
}
