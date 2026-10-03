using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application.Pricing;

public sealed record CustomerGroupDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsWholesale,
    int SortOrder,
    bool IsActive,
    decimal? MinimumOrderAmount,
    int? MinimumOrderQuantity);

public sealed record SaveCustomerGroupCommand(
    string Name,
    string Code,
    string? Description,
    bool IsWholesale,
    int SortOrder,
    bool IsActive,
    decimal? MinimumOrderAmount,
    int? MinimumOrderQuantity);

public sealed record PriceRuleDto(
    Guid Id,
    string Name,
    PriceRuleType RuleType,
    Guid? ProductId,
    Guid? VariationId,
    Guid? CustomerGroupId,
    int? MinQuantity,
    int? MaxQuantity,
    decimal? FixedPrice,
    decimal? DiscountPercent,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    bool IsActive,
    int Priority);

public sealed record SavePriceRuleCommand(
    string Name,
    PriceRuleType RuleType,
    Guid? ProductId,
    Guid? VariationId,
    Guid? CustomerGroupId,
    int? MinQuantity,
    int? MaxQuantity,
    decimal? FixedPrice,
    decimal? DiscountPercent,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    bool IsActive,
    int Priority);

public sealed record PriceQuoteRequest(
    Guid ProductId,
    Guid? VariationId,
    int Quantity,
    Guid? CustomerGroupId);

public sealed record PriceQuoteResult(
    decimal RegularPrice,
    decimal UnitPrice,
    decimal LineTotal,
    Guid? AppliedRuleId,
    string? AppliedRuleName);
