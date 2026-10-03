namespace CMS.Modules.Shop.Application.Reviews;

public sealed record ReviewListItemDto(
    Guid Id,
    Guid ProductId,
    string ProductTitle,
    string AuthorName,
    int Rating,
    string Comment,
    bool IsApproved,
    string? AdminResponse,
    DateTime CreatedAtUtc);

public sealed record SubmitReviewCommand(
    Guid ProductId,
    string? UserId,
    string AuthorName,
    int Rating,
    string Comment);

public sealed record ModerateReviewCommand(bool Approve, string? AdminResponse);
