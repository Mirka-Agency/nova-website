using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Domain.Entities;

public class ShippingMethod : BaseEntity
{
    private readonly List<ShippingRate> _rates = [];

    private ShippingMethod()
    {
    }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ShippingCalculationType CalculationType { get; private set; }
    public decimal FixedCost { get; private set; }
    public decimal? FreeShippingMinAmount { get; private set; }
    public string? EstimatedDeliveryText { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }
    public IReadOnlyCollection<ShippingRate> Rates => _rates;

    public static ShippingMethod Create(
        string name,
        string? description,
        ShippingCalculationType calculationType,
        decimal fixedCost,
        decimal? freeShippingMinAmount,
        string? estimatedDeliveryText,
        int sortOrder)
    {
        var method = new ShippingMethod();
        method.Apply(name, description, calculationType, fixedCost, freeShippingMinAmount, estimatedDeliveryText, true, sortOrder);
        return method;
    }

    public void Update(
        string name,
        string? description,
        ShippingCalculationType calculationType,
        decimal fixedCost,
        decimal? freeShippingMinAmount,
        string? estimatedDeliveryText,
        bool isActive,
        int sortOrder)
    {
        Apply(name, description, calculationType, fixedCost, freeShippingMinAmount, estimatedDeliveryText, isActive, sortOrder);
        Touch();
    }

    public ShippingRate AddRate(string? province, string? city, decimal? minWeight, decimal? maxWeight, decimal cost)
    {
        var rate = ShippingRate.Create(Id, province, city, minWeight, maxWeight, cost);
        _rates.Add(rate);
        Touch();
        return rate;
    }

    public void ClearRates()
    {
        _rates.Clear();
        Touch();
    }

    private void Apply(
        string name,
        string? description,
        ShippingCalculationType calculationType,
        decimal fixedCost,
        decimal? freeShippingMinAmount,
        string? estimatedDeliveryText,
        bool isActive,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام روش ارسال الزامی است.");
        if (!Enum.IsDefined(calculationType))
            throw new DomainException("نوع محاسبه ارسال نامعتبر است.");
        if (fixedCost < 0)
            throw new DomainException("هزینه ارسال نمی‌تواند منفی باشد.");
        if (freeShippingMinAmount is < 0)
            throw new DomainException("حداقل مبلغ ارسال رایگان نامعتبر است.");

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CalculationType = calculationType;
        FixedCost = fixedCost;
        FreeShippingMinAmount = freeShippingMinAmount;
        EstimatedDeliveryText = string.IsNullOrWhiteSpace(estimatedDeliveryText) ? null : estimatedDeliveryText.Trim();
        IsActive = isActive;
        SortOrder = Math.Max(0, sortOrder);
    }
}

public class ShippingRate : BaseEntity
{
    private ShippingRate()
    {
    }

    public Guid ShippingMethodId { get; private set; }
    public ShippingMethod ShippingMethod { get; private set; } = null!;
    public string? Province { get; private set; }
    public string? City { get; private set; }
    public decimal? MinWeight { get; private set; }
    public decimal? MaxWeight { get; private set; }
    public decimal Cost { get; private set; }

    public static ShippingRate Create(
        Guid methodId,
        string? province,
        string? city,
        decimal? minWeight,
        decimal? maxWeight,
        decimal cost)
    {
        if (cost < 0)
            throw new DomainException("هزینه ارسال نمی‌تواند منفی باشد.");

        return new ShippingRate
        {
            ShippingMethodId = methodId,
            Province = string.IsNullOrWhiteSpace(province) ? null : province.Trim(),
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            MinWeight = minWeight,
            MaxWeight = maxWeight,
            Cost = cost
        };
    }
}
