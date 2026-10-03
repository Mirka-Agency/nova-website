using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application.Promotions;

public sealed record CouponDto(
    Guid Id,
    string Code,
    string Name,
    DiscountType DiscountType,
    decimal Amount,
    Guid? ProductId,
    Guid? CategoryId,
    int? MinQuantity,
    decimal? MinOrderAmount,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? MaxRedemptions,
    int RedemptionCount,
    bool IsActive,
    string? AllowedUserId);

public sealed record SaveCouponCommand(
    string Code,
    string Name,
    DiscountType DiscountType,
    decimal Amount,
    Guid? ProductId,
    Guid? CategoryId,
    int? MinQuantity,
    decimal? MinOrderAmount,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    int? MaxRedemptions,
    bool IsActive,
    string? AllowedUserId = null);
