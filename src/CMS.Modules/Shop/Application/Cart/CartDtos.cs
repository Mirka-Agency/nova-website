using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application.Cart;

public sealed record CartItemDto(
    Guid ProductId,
    Guid? VariationId,
    string Title,
    string Slug,
    string? Sku,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    string? CoverImageUrl,
    int? MaxQuantity = null)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed record CartDto(
    IReadOnlyList<CartItemDto> Items,
    string Currency,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal ShippingAmount,
    decimal VatAmount,
    decimal? VatPercent,
    decimal Total,
    string? CouponCode,
    Guid? ShippingMethodId,
    string? ValidationMessage)
{
    public int ItemCount => Items.Sum(i => i.Quantity);
}

public sealed record AddToCartCommand(Guid ProductId, Guid? VariationId = null, int Quantity = 1);
public sealed record UpdateCartItemCommand(Guid ProductId, Guid? VariationId, int Quantity);
public sealed record ApplyCouponCommand(string? Code);
public sealed record SetCartShippingCommand(Guid? ShippingMethodId);
