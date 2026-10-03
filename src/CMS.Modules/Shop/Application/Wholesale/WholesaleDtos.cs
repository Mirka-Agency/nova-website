using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application.Wholesale;

public sealed record WholesaleRequestListItemDto(
    Guid Id,
    string CompanyName,
    string ContactName,
    string Email,
    string Phone,
    WholesaleRequestStatus Status,
    DateTime CreatedAtUtc);

public sealed record WholesaleRequestDetailDto(
    Guid Id,
    string? UserId,
    string CompanyName,
    string BusinessInfo,
    string ContactName,
    string Phone,
    string Email,
    string Address,
    string? DocumentUrlsJson,
    WholesaleRequestStatus Status,
    string? AdminNotes,
    Guid? AssignedGroupId,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc);

public sealed record SubmitWholesaleRequestCommand(
    string? UserId,
    string CompanyName,
    string BusinessInfo,
    string ContactName,
    string Phone,
    string Email,
    string Address,
    string? DocumentUrlsJson);

public sealed record ReviewWholesaleRequestCommand(Guid? GroupId, string? AdminNotes);
