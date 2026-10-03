using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Domain.Entities;

public class CustomerGroup : BaseEntity
{
    private CustomerGroup()
    {
    }

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsWholesale { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public decimal? MinimumOrderAmount { get; private set; }
    public int? MinimumOrderQuantity { get; private set; }

    public static CustomerGroup Create(
        string name,
        string code,
        string? description,
        bool isWholesale,
        int sortOrder,
        decimal? minimumOrderAmount = null,
        int? minimumOrderQuantity = null)
    {
        var group = new CustomerGroup();
        group.Apply(name, code, description, isWholesale, sortOrder, true, minimumOrderAmount, minimumOrderQuantity);
        return group;
    }

    public void Update(
        string name,
        string code,
        string? description,
        bool isWholesale,
        int sortOrder,
        bool isActive,
        decimal? minimumOrderAmount,
        int? minimumOrderQuantity)
    {
        Apply(name, code, description, isWholesale, sortOrder, isActive, minimumOrderAmount, minimumOrderQuantity);
        Touch();
    }

    private void Apply(
        string name,
        string code,
        string? description,
        bool isWholesale,
        int sortOrder,
        bool isActive,
        decimal? minimumOrderAmount,
        int? minimumOrderQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام گروه مشتری الزامی است.");
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("کد گروه مشتری الزامی است.");

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsWholesale = isWholesale;
        SortOrder = Math.Max(0, sortOrder);
        IsActive = isActive;
        MinimumOrderAmount = minimumOrderAmount;
        MinimumOrderQuantity = minimumOrderQuantity;
    }
}

public class CustomerGroupMembership : BaseEntity
{
    private CustomerGroupMembership()
    {
    }

    public string UserId { get; private set; } = string.Empty;
    public Guid CustomerGroupId { get; private set; }
    public CustomerGroup CustomerGroup { get; private set; } = null!;

    public static CustomerGroupMembership Create(string userId, Guid customerGroupId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("شناسه کاربر الزامی است.");

        return new CustomerGroupMembership
        {
            UserId = userId.Trim(),
            CustomerGroupId = customerGroupId
        };
    }

    public void ChangeGroup(Guid customerGroupId)
    {
        CustomerGroupId = customerGroupId;
        Touch();
    }
}

public class PriceRule : BaseEntity
{
    private PriceRule()
    {
    }

    public string Name { get; private set; } = string.Empty;
    public PriceRuleType RuleType { get; private set; }
    public Guid? ProductId { get; private set; }
    public Product? Product { get; private set; }
    public Guid? VariationId { get; private set; }
    public Guid? CustomerGroupId { get; private set; }
    public CustomerGroup? CustomerGroup { get; private set; }
    public int? MinQuantity { get; private set; }
    public int? MaxQuantity { get; private set; }
    public decimal? FixedPrice { get; private set; }
    public decimal? DiscountPercent { get; private set; }
    public DateTime? StartsAtUtc { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int Priority { get; private set; }

    public static PriceRule Create(
        string name,
        PriceRuleType ruleType,
        Guid? productId,
        Guid? variationId,
        Guid? customerGroupId,
        int? minQuantity,
        int? maxQuantity,
        decimal? fixedPrice,
        decimal? discountPercent,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        int priority)
    {
        var rule = new PriceRule();
        rule.Apply(name, ruleType, productId, variationId, customerGroupId, minQuantity, maxQuantity,
            fixedPrice, discountPercent, startsAtUtc, endsAtUtc, true, priority);
        return rule;
    }

    public void Update(
        string name,
        PriceRuleType ruleType,
        Guid? productId,
        Guid? variationId,
        Guid? customerGroupId,
        int? minQuantity,
        int? maxQuantity,
        decimal? fixedPrice,
        decimal? discountPercent,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        bool isActive,
        int priority)
    {
        Apply(name, ruleType, productId, variationId, customerGroupId, minQuantity, maxQuantity,
            fixedPrice, discountPercent, startsAtUtc, endsAtUtc, isActive, priority);
        Touch();
    }

    public bool IsEffective(DateTime utcNow) =>
        IsActive
        && (!StartsAtUtc.HasValue || StartsAtUtc <= utcNow)
        && (!EndsAtUtc.HasValue || EndsAtUtc >= utcNow);

    private void Apply(
        string name,
        PriceRuleType ruleType,
        Guid? productId,
        Guid? variationId,
        Guid? customerGroupId,
        int? minQuantity,
        int? maxQuantity,
        decimal? fixedPrice,
        decimal? discountPercent,
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        bool isActive,
        int priority)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام قاعده قیمت الزامی است.");
        if (!Enum.IsDefined(ruleType))
            throw new DomainException("نوع قاعده قیمت نامعتبر است.");
        if (fixedPrice is < 0)
            throw new DomainException("قیمت ثابت نمی‌تواند منفی باشد.");
        if (discountPercent is < 0 or > 100)
            throw new DomainException("درصد تخفیف نامعتبر است.");

        Name = name.Trim();
        RuleType = ruleType;
        ProductId = productId;
        VariationId = variationId;
        CustomerGroupId = customerGroupId;
        MinQuantity = minQuantity;
        MaxQuantity = maxQuantity;
        FixedPrice = fixedPrice;
        DiscountPercent = discountPercent;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        IsActive = isActive;
        Priority = priority;
    }
}
