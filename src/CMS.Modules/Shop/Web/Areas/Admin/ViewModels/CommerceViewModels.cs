using System.ComponentModel.DataAnnotations;
using CMS.Modules.Shop.Application.Catalog;
using CMS.Modules.Shop.Application.Pricing;
using CMS.Modules.Shop.Application.Promotions;
using CMS.Modules.Shop.Application.Reports;
using CMS.Modules.Shop.Application.Reviews;
using CMS.Modules.Shop.Application.Shipping;
using CMS.Modules.Shop.Application.Wholesale;
using CMS.Modules.Shop.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CMS.Modules.Shop.Web.Areas.Admin.ViewModels;

public sealed class BrandFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "نام الزامی است.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Slug { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(500_000)]
    public string? Content { get; set; }

    [StringLength(1000)]
    public string? LogoUrl { get; set; }

    [StringLength(200)]
    public string? MetaTitle { get; set; }

    [StringLength(500)]
    public string? MetaDescription { get; set; }

    [StringLength(500)]
    public string? SeoKeywords { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class AttributeFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "نام الزامی است.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Slug { get; set; }

    public int SortOrder { get; set; }

    public List<AttributeValueFormViewModel> Values { get; set; } = [];
}

public sealed class AttributeValueFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "مقدار الزامی است.")]
    [StringLength(100)]
    public string Value { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Slug { get; set; }

    public int SortOrder { get; set; }
}

public sealed class CustomerGroupFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "نام الزامی است.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "کد الزامی است.")]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool IsWholesale { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? MinimumOrderAmount { get; set; }
    public int? MinimumOrderQuantity { get; set; }
}

public sealed class PriceRuleFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "نام الزامی است.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public PriceRuleType RuleType { get; set; } = PriceRuleType.CustomerGroup;
    public Guid? ProductId { get; set; }
    public Guid? CustomerGroupId { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public decimal? FixedPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }

    public IEnumerable<SelectListItem> GroupOptions { get; set; } = [];
    public IEnumerable<SelectListItem> RuleTypeOptions { get; set; } = [];
}

public sealed class WholesaleDetailViewModel
{
    public WholesaleRequestDetailDto Request { get; init; } = null!;
    public IEnumerable<SelectListItem> GroupOptions { get; set; } = [];
    public Guid? AssignedGroupId { get; set; }
    public string? AdminNotes { get; set; }
}

public sealed class CouponFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "کد الزامی است.")]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "نام الزامی است.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

    [Range(0.01, double.MaxValue, ErrorMessage = "مقدار تخفیف باید بیشتر از صفر باشد.")]
    public decimal Amount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "حداقل مبلغ خرید نامعتبر است.")]
    public decimal? MinOrderAmount { get; set; }

    public int? MaxRedemptions { get; set; }

    /// <summary>Optional customer email; coupon is locked to that Identity user when set.</summary>
    [EmailAddress(ErrorMessage = "ایمیل کاربر نامعتبر است.")]
    [StringLength(256)]
    public string? AllowedUserEmail { get; set; }

    /// <summary>Jalali Iran wall-clock text (e.g. 1404/05/19 14:30); converted to UTC on save.</summary>
    public string? StartsAtLocal { get; set; }

    /// <summary>Jalali Iran wall-clock text (e.g. 1404/05/19 14:30); converted to UTC on save.</summary>
    public string? EndsAtLocal { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem> DiscountTypeOptions { get; set; } = [];
}

public sealed class ShippingRateFormViewModel
{
    public string? Province { get; set; }
    public string? City { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MinWeight { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? MaxWeight { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Cost { get; set; }
}

public sealed class ShippingMethodFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "نام الزامی است.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public ShippingCalculationType CalculationType { get; set; } = ShippingCalculationType.Fixed;

    [Range(0, double.MaxValue)]
    public decimal FixedCost { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? FreeShippingMinAmount { get; set; }

    [StringLength(200)]
    public string? EstimatedDeliveryText { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public List<ShippingRateFormViewModel> Rates { get; set; } = [];
    public IEnumerable<SelectListItem> CalculationTypeOptions { get; set; } = [];
}

public sealed class PaymentProviderFormViewModel
{
    public Guid Id { get; set; }
    public PaymentProviderType ProviderType { get; set; }

    [Required(ErrorMessage = "نام نمایشی الزامی است.")]
    [StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }
    public bool IsSandbox { get; set; }

    /// <summary>False in Production — sandbox cannot be enabled.</summary>
    public bool SandboxAllowed { get; set; } = true;

    [Range(0, 1000)]
    public int SortOrder { get; set; }

    [StringLength(100)]
    public string? MerchantId { get; set; }

    [StringLength(100)]
    public string? TerminalId { get; set; }

    [StringLength(200)]
    public string? Username { get; set; }

    [StringLength(200)]
    public string? Password { get; set; }

    [StringLength(200)]
    public string? ClientId { get; set; }

    [StringLength(200)]
    public string? ClientSecret { get; set; }

    public string CallbackUrl { get; set; } = string.Empty;
}

public sealed class CommerceReportViewModel
{
    public CommerceReportDto Report { get; init; } = null!;
}

public sealed class ProductVariationFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "کد SKU")]
    [Required(ErrorMessage = "کد SKU الزامی است.")]
    [StringLength(100, ErrorMessage = "کد SKU حداکثر ۱۰۰ نویسه می‌تواند باشد.")]
    public string Sku { get; set; } = string.Empty;

    [Display(Name = "خلاصه ویژگی‌ها")]
    [StringLength(500, ErrorMessage = "خلاصه ویژگی‌ها حداکثر ۵۰۰ نویسه می‌تواند باشد.")]
    public string? AttributeSummary { get; set; }

    [Display(Name = "قیمت")]
    [Range(0, double.MaxValue, ErrorMessage = "قیمت نمی‌تواند منفی باشد.")]
    public decimal Price { get; set; }

    [Display(Name = "قیمت فروش")]
    [Range(0, double.MaxValue, ErrorMessage = "قیمت فروش نمی‌تواند منفی باشد.")]
    public decimal? SalePrice { get; set; }

    [Display(Name = "موجودی")]
    [Range(0, int.MaxValue, ErrorMessage = "موجودی نمی‌تواند منفی باشد.")]
    public int? StockQuantity { get; set; }

    [Display(Name = "همیشه موجود")]
    public bool UnlimitedStock { get; set; }

    [Display(Name = "تصویر")]
    [StringLength(1000, ErrorMessage = "آدرس تصویر حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string? ImageUrl { get; set; }

    [Display(Name = "وزن")]
    [Range(0, double.MaxValue, ErrorMessage = "وزن نمی‌تواند منفی باشد.")]
    public decimal? Weight { get; set; }

    [Display(Name = "وضعیت")]
    public VariationStatus Status { get; set; } = VariationStatus.Active;
    public IEnumerable<SelectListItem> StatusOptions { get; set; } = [];
}

public sealed class ProductVariationsPageViewModel
{
    public Guid ProductId { get; init; }
    public string ProductTitle { get; init; } = string.Empty;
    public IReadOnlyList<CMS.Modules.Shop.Application.Products.ProductVariationDto> Variations { get; init; } = [];
    public ProductVariationFormViewModel Form { get; set; } = new();
    public bool IsEditing => Form.Id.HasValue;
}

public sealed class ProductSpecificationFormViewModel
{
    [Required(ErrorMessage = "کلید الزامی است.")]
    [StringLength(200)]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "مقدار الزامی است.")]
    [StringLength(1000)]
    public string Value { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

public sealed class ProductSpecificationsPageViewModel
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public List<ProductSpecificationFormViewModel> Items { get; set; } = [];
}
