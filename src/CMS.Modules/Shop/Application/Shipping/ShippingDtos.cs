using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application.Shipping;

public sealed record ShippingRateDto(
    Guid Id,
    string? Province,
    string? City,
    decimal? MinWeight,
    decimal? MaxWeight,
    decimal Cost);

public sealed record ShippingMethodDto(
    Guid Id,
    string Name,
    string? Description,
    ShippingCalculationType CalculationType,
    decimal FixedCost,
    decimal? FreeShippingMinAmount,
    string? EstimatedDeliveryText,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<ShippingRateDto> Rates);

public sealed record SaveShippingRateCommand(
    string? Province,
    string? City,
    decimal? MinWeight,
    decimal? MaxWeight,
    decimal Cost);

public sealed record SaveShippingMethodCommand(
    string Name,
    string? Description,
    ShippingCalculationType CalculationType,
    decimal FixedCost,
    decimal? FreeShippingMinAmount,
    string? EstimatedDeliveryText,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<SaveShippingRateCommand>? Rates);

public sealed record ShippingQuoteRequest(
    Guid MethodId,
    decimal Subtotal,
    decimal TotalWeight,
    string? Province,
    string? City);

public sealed record ShippingQuoteResult(Guid MethodId, string Name, decimal Cost, string? EstimatedDeliveryText);
