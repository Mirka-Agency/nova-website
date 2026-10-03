using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Modules.Shop.Domain.Entities;

public class ShoppingCart : BaseEntity
{
    private readonly List<ShoppingCartItem> _items = [];

    private ShoppingCart()
    {
    }

    public string? UserId { get; private set; }
    public string? SessionId { get; private set; }
    public string? CouponCode { get; private set; }
    public Guid? ShippingMethodId { get; private set; }
    public IReadOnlyCollection<ShoppingCartItem> Items => _items;

    public static ShoppingCart CreateForUser(string userId) =>
        new() { UserId = userId.Trim() };

    public static ShoppingCart CreateForSession(string sessionId) =>
        new() { SessionId = sessionId.Trim() };

    public ShoppingCartItem UpsertItem(Guid productId, Guid? variationId, int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("تعداد باید مثبت باشد.");

        var existing = _items.FirstOrDefault(i => i.ProductId == productId && i.VariationId == variationId);
        if (existing is null)
        {
            existing = ShoppingCartItem.Create(Id, productId, variationId, quantity);
            _items.Add(existing);
        }
        else
        {
            existing.SetQuantity(existing.Quantity + quantity);
        }

        Touch();
        return existing;
    }

    public void SetItemQuantity(Guid productId, Guid? variationId, int quantity)
    {
        var existing = _items.FirstOrDefault(i => i.ProductId == productId && i.VariationId == variationId)
            ?? throw new DomainException("آیتم سبد یافت نشد.");

        if (quantity <= 0)
            _items.Remove(existing);
        else
            existing.SetQuantity(quantity);

        Touch();
    }

    public void RemoveItem(Guid productId, Guid? variationId)
    {
        _items.RemoveAll(i => i.ProductId == productId && i.VariationId == variationId);
        Touch();
    }

    public void Clear()
    {
        _items.Clear();
        CouponCode = null;
        Touch();
    }

    public void ApplyCoupon(string? code)
    {
        CouponCode = string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
        Touch();
    }

    public void SetShippingMethod(Guid? shippingMethodId)
    {
        ShippingMethodId = shippingMethodId;
        Touch();
    }
}

public class ShoppingCartItem : BaseEntity
{
    private ShoppingCartItem()
    {
    }

    public Guid CartId { get; private set; }
    public ShoppingCart Cart { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public Guid? VariationId { get; private set; }
    public int Quantity { get; private set; }

    public static ShoppingCartItem Create(Guid cartId, Guid productId, Guid? variationId, int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("تعداد باید مثبت باشد.");

        return new ShoppingCartItem
        {
            CartId = cartId,
            ProductId = productId,
            VariationId = variationId,
            Quantity = quantity
        };
    }

    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("تعداد باید مثبت باشد.");
        Quantity = quantity;
        Touch();
    }
}
