using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Domain.Entities;

public class Coupon : BaseEntity
{
    private Coupon()
    {
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DiscountType DiscountType { get; private set; }
    public decimal Amount { get; private set; }
    public Guid? ProductId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public int? MinQuantity { get; private set; }
    public decimal? MinOrderAmount { get; private set; }
    public DateTime? StartsAtUtc { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }
    public int? MaxRedemptions { get; private set; }
    public int RedemptionCount { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>When set, only this Identity user may redeem the coupon.</summary>
    public string? AllowedUserId { get; private set; }

    public static Coupon Create(
        string code,
        string name,
        DiscountType discountType,
        decimal amount,
        Guid? productId,
        Guid? categoryId,
        int? minQuantity,
        decimal? minOrderAmount,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        int? maxRedemptions,
        string? allowedUserId = null)
    {
        var coupon = new Coupon();
        coupon.Apply(code, name, discountType, amount, productId, categoryId, minQuantity, minOrderAmount,
            startsAtUtc, endsAtUtc, maxRedemptions, true, allowedUserId);
        return coupon;
    }

    public void Update(
        string code,
        string name,
        DiscountType discountType,
        decimal amount,
        Guid? productId,
        Guid? categoryId,
        int? minQuantity,
        decimal? minOrderAmount,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        int? maxRedemptions,
        bool isActive,
        string? allowedUserId = null)
    {
        Apply(code, name, discountType, amount, productId, categoryId, minQuantity, minOrderAmount,
            startsAtUtc, endsAtUtc, maxRedemptions, isActive, allowedUserId);
        Touch();
    }

    public bool IsEffective(DateTime utcNow) =>
        IsActive
        && (!StartsAtUtc.HasValue || StartsAtUtc <= utcNow)
        && (!EndsAtUtc.HasValue || EndsAtUtc >= utcNow)
        && (!MaxRedemptions.HasValue || RedemptionCount < MaxRedemptions);

    public bool IsAllowedForUser(string? userId) =>
        string.IsNullOrWhiteSpace(AllowedUserId)
        || (!string.IsNullOrWhiteSpace(userId)
            && string.Equals(AllowedUserId, userId, StringComparison.Ordinal));

    public void IncrementRedemption()
    {
        if (!TryRedeem())
            throw new DomainException("ظرفیت استفاده از این کد تخفیف به پایان رسیده است.");
    }

    public bool TryRedeem()
    {
        if (MaxRedemptions.HasValue && RedemptionCount >= MaxRedemptions.Value)
            return false;

        RedemptionCount++;
        Touch();
        return true;
    }

    public void ReverseRedemption()
    {
        if (RedemptionCount <= 0)
            return;

        RedemptionCount--;
        Touch();
    }

    private void Apply(
        string code,
        string name,
        DiscountType discountType,
        decimal amount,
        Guid? productId,
        Guid? categoryId,
        int? minQuantity,
        decimal? minOrderAmount,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        int? maxRedemptions,
        bool isActive,
        string? allowedUserId)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("کد کوپن الزامی است.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام کوپن الزامی است.");
        if (!Enum.IsDefined(discountType))
            throw new DomainException("نوع تخفیف نامعتبر است.");
        if (amount <= 0)
            throw new DomainException("مقدار تخفیف باید بیشتر از صفر باشد.");
        if (discountType == DiscountType.Percentage && amount > 100)
            throw new DomainException("برای تخفیف درصدی، مقدار باید حداکثر ۱۰۰ باشد.");
        if (minOrderAmount.HasValue && minOrderAmount.Value < 0)
            throw new DomainException("حداقل مبلغ خرید نامعتبر است.");
        if (startsAtUtc.HasValue && endsAtUtc.HasValue && endsAtUtc < startsAtUtc)
            throw new DomainException("تاریخ پایان باید بعد از تاریخ شروع باشد.");

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        DiscountType = discountType;
        Amount = amount;
        ProductId = productId;
        CategoryId = categoryId;
        MinQuantity = minQuantity;
        MinOrderAmount = minOrderAmount;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        MaxRedemptions = maxRedemptions;
        IsActive = isActive;
        AllowedUserId = string.IsNullOrWhiteSpace(allowedUserId) ? null : allowedUserId.Trim();
    }
}

public class CouponRedemption : BaseEntity
{
    private CouponRedemption()
    {
    }

    public Guid CouponId { get; private set; }
    public Coupon Coupon { get; private set; } = null!;
    public Guid OrderId { get; private set; }
    public string? UserId { get; private set; }
    public decimal DiscountAmount { get; private set; }

    public static CouponRedemption Create(Guid couponId, Guid orderId, string? userId, decimal discountAmount) =>
        new()
        {
            CouponId = couponId,
            OrderId = orderId,
            UserId = userId,
            DiscountAmount = discountAmount
        };
}
