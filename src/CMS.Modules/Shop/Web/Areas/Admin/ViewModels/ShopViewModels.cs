using System.ComponentModel.DataAnnotations;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CMS.Modules.Shop.Web.Areas.Admin.ViewModels;

public sealed class ProductIndexViewModel
{
    public IReadOnlyList<ProductListItemViewModel> Items { get; init; } = [];
    public string? Search { get; init; }
    public ProductStatus? Status { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? BrandId { get; init; }
    public ProductType? ProductType { get; init; }
    public bool? IsAvailable { get; init; }
    public bool? IsPurchasable { get; init; }
    public string? Stock { get; init; }
    public string? Sort { get; init; }
    public int Page { get; init; } = 1;
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public bool ShowPurchasable { get; init; }
    public IEnumerable<SelectListItem> CategoryOptions { get; init; } = [];
    public IEnumerable<SelectListItem> BrandOptions { get; init; } = [];
}

public sealed class ProductListItemViewModel
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string PriceDisplay { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public bool IsPurchasable { get; init; }
    public string? CategoryName { get; init; }
    public string? CoverImageUrl { get; init; }
    public int VariationCount { get; init; }
    public int SpecificationCount { get; init; }
    public string StockDisplay { get; init; } = "—";
}

public sealed class ProductFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "عنوان الزامی است.")]
    [StringLength(300, ErrorMessage = "عنوان حداکثر ۳۰۰ نویسه می‌تواند باشد.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "نامک حداکثر ۳۰۰ نویسه می‌تواند باشد.")]
    public string? Slug { get; set; }

    [StringLength(100, ErrorMessage = "کد محصول حداکثر ۱۰۰ نویسه می‌تواند باشد.")]
    public string? Sku { get; set; }

    [StringLength(2000, ErrorMessage = "توضیح کوتاه خیلی طولانی است.")]
    public string? ShortDescription { get; set; }

    [StringLength(20_000, ErrorMessage = "توضیحات خیلی طولانی است.")]
    public string? Description { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "قیمت نمی‌تواند منفی باشد.")]
    public decimal Price { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "قیمت فروش نمی‌تواند منفی باشد.")]
    public decimal? SalePrice { get; set; }

    [StringLength(8, ErrorMessage = "کد ارز خیلی طولانی است.")]
    public string Currency { get; set; } = "IRR";

    public bool IsAvailable { get; set; } = true;
    public bool IsPurchasable { get; set; }

    /// <summary>When false (catalog-only shop mode), the purchasable checkbox is hidden.</summary>
    public bool ShowPurchasableOption { get; set; } = true;

    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }

    [StringLength(1000, ErrorMessage = "آدرس تصویر شاخص حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string? CoverImageUrl { get; set; }

    [StringLength(1000)]
    public string? VideoUrl { get; set; }

    public int? StockQuantity { get; set; }
    public bool UnlimitedStock { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "وزن نمی‌تواند منفی باشد.")]
    public decimal? Weight { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "حداقل تعداد عمده باید حداقل ۱ باشد.")]
    public int? WholesaleMinimumOrderQuantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "حداقل مبلغ عمده نمی‌تواند منفی باشد.")]
    public decimal? WholesaleMinimumOrderAmount { get; set; }

    [StringLength(200)]
    public string? MetaTitle { get; set; }

    [StringLength(500)]
    public string? MetaDescription { get; set; }

    [StringLength(500)]
    public string? SeoKeywords { get; set; }

    public IFormFile? CoverImage { get; set; }

    public List<ProductImageFormViewModel> Images { get; set; } = [];

    public IEnumerable<SelectListItem> CategoryOptions { get; set; } = [];
    public IEnumerable<SelectListItem> BrandOptions { get; set; } = [];
    public IEnumerable<SelectListItem> StatusOptions { get; set; } = [];
    public IEnumerable<SelectListItem> CurrencyOptions { get; set; } = [];
}

public sealed class ProductImportViewModel
{
    public IFormFile? File { get; set; }
    public ProductImportResultViewModel? Result { get; set; }
}

public sealed class ProductImportResultViewModel
{
    public int TotalRows { get; init; }
    public int CreatedCount { get; init; }
    public int FailedCount { get; init; }
    public IReadOnlyList<ProductImportRowErrorViewModel> Errors { get; init; } = [];
}

public sealed class ProductImportRowErrorViewModel
{
    public int RowNumber { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class ProductImageFormViewModel
{
    [StringLength(1000)]
    public string Url { get; set; } = string.Empty;

    [StringLength(300)]
    public string? AltText { get; set; }

    public int SortOrder { get; set; }

    public bool IsMain { get; set; }
}

public sealed class ShopCategoryListItemViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public int ProductCount { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CategoriesAndBrandsPageViewModel
{
    public string ActiveTab { get; set; } = "categories";
    public IReadOnlyList<ShopCategoryListItemViewModel> Categories { get; set; } = [];
    public IReadOnlyList<CMS.Modules.Shop.Application.Catalog.BrandListItemDto> Brands { get; set; } = [];
}

public sealed class ShopCategoryFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "نام الزامی است.")]
    [StringLength(200, ErrorMessage = "نام حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "نامک حداکثر ۲۰۰ نویسه می‌تواند باشد.")]
    public string? Slug { get; set; }

    [StringLength(4000, ErrorMessage = "توضیحات خیلی طولانی است.")]
    public string? Description { get; set; }

    [StringLength(500_000)]
    public string? Content { get; set; }

    [StringLength(1000, ErrorMessage = "آدرس تصویر حداکثر ۱۰۰۰ نویسه می‌تواند باشد.")]
    public string? ImageUrl { get; set; }

    public IFormFile? Image { get; set; }

    [StringLength(200)]
    public string? MetaTitle { get; set; }

    [StringLength(500)]
    public string? MetaDescription { get; set; }

    [StringLength(500)]
    public string? SeoKeywords { get; set; }

    [Range(0, 10_000, ErrorMessage = "ترتیب نمایش نامعتبر است.")]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class ShopSettingsViewModel
{
    public ShopMode Mode { get; set; } = ShopMode.CatalogOnly;
    public string ModeDisplay { get; set; } = string.Empty;
    public bool IsCatalogOnly { get; set; }
    public bool EcommerceEnabled { get; set; }
    public string ActiveTab { get; set; } = "general";
    public string PaymentProviderName { get; set; } = string.Empty;
    public IEnumerable<SelectListItem> ModeOptions { get; set; } = [];

    public SalesAudience SalesAudience { get; set; } = SalesAudience.RetailOnly;
    public bool EnableOnlinePayment { get; set; } = true;
    public bool EnableBankTransfer { get; set; } = true;
    public string? BankAccountHolderName { get; set; }
    public string? BankName { get; set; }
    public string? BankCardNumber { get; set; }
    public string? BankShebaNumber { get; set; }
    public string? BankTransferInstructions { get; set; }
    public bool ShowPricesToGuests { get; set; } = true;
    public bool EnableReviews { get; set; } = true;
    public bool EnableCoupons { get; set; } = true;
    public bool EnableWholesaleInvoice { get; set; } = true;
    public decimal? WholesaleMinimumOrderAmount { get; set; }
    public int? WholesaleMinimumOrderQuantity { get; set; }
    public int PendingPaymentTimeoutMinutes { get; set; } = 30;
    public int AbandonedPaymentReminderMinutes { get; set; } = 3;
    public bool EnableVat { get; set; }
    public decimal VatPercent { get; set; } = ShopSettings.DefaultVatPercent;
    public IEnumerable<SelectListItem> SalesAudienceOptions { get; set; } = [];
    public string? SellerName { get; set; }
    public string? SellerPhone { get; set; }
    public string? SellerProvince { get; set; }
    public string? SellerCity { get; set; }
    public string? SellerAddress { get; set; }
    public string? SellerPostalCode { get; set; }
    public string? SellerEmail { get; set; }
    public string? SellerNationalId { get; set; }
    public string? SellerEconomicCode { get; set; }
    public string? SellerRegistrationNumber { get; set; }
    public bool InvoiceShowSellerName { get; set; } = true;
    public bool InvoiceShowSellerNationalId { get; set; }
    public bool InvoiceShowSellerEconomicCode { get; set; }
    public bool InvoiceShowSellerRegistrationNumber { get; set; }
    public bool InvoiceShowSellerPhone { get; set; } = true;
    public bool InvoiceShowSellerEmail { get; set; }
    public bool InvoiceShowSellerCity { get; set; }
    public bool InvoiceShowSellerPostalCode { get; set; }
    public bool InvoiceShowSellerAddress { get; set; } = true;
    public bool InvoiceShowBuyerCustomerName { get; set; } = true;
    public bool InvoiceShowBuyerCustomerEmail { get; set; }
    public bool InvoiceShowBuyerCustomerPhone { get; set; }
    public bool InvoiceShowBuyerRecipientName { get; set; } = true;
    public bool InvoiceShowBuyerRecipientPhone { get; set; } = true;
    public bool InvoiceShowBuyerShippingCity { get; set; }
    public bool InvoiceShowBuyerShippingAddress { get; set; } = true;
    public bool InvoiceShowBuyerIsWholesale { get; set; }
}

public sealed class ShopHomeViewModel
{
    public int ProductCount { get; init; }
    public int PublishedProductCount { get; init; }
    public int CategoryCount { get; init; }
    public int OrderCount { get; init; }
    public int ProcessingOrderCount { get; init; }
    public string ModeDisplay { get; init; } = string.Empty;
    public IReadOnlyList<OrderListItemViewModel> RecentOrders { get; init; } = [];
}

public sealed class OrderIndexViewModel
{
    public IReadOnlyList<OrderListItemViewModel> Items { get; init; } = [];
    public string? Search { get; init; }
    public string? Status { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public bool? IsWholesale { get; init; }
    public string? FromLocal { get; init; }
    public string? ToLocal { get; init; }
    public string? Sort { get; init; }
    public int Page { get; init; } = 1;
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public string PageTitle { get; init; } = string.Empty;
}

public sealed class OrderListItemViewModel
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string StatusDisplay { get; init; } = string.Empty;
    public string TotalDisplay { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public OrderStatus Status { get; init; }
    public DateTime? PaymentExpiresAtUtc { get; init; }
}

public sealed class OrderDetailViewModel
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public OrderStatus Status { get; set; }
    public string StatusDisplay { get; init; } = string.Empty;
    public string PaymentStatusDisplay { get; init; } = string.Empty;
    public PaymentStatus PaymentStatus { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public string? PaymentMethodDisplay { get; init; }
    public bool CanConfirmBankTransfer { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string? RecipientName { get; init; }
    public string? RecipientPhone { get; init; }
    public string? ShippingAddress { get; init; }
    public string? ShippingCity { get; init; }
    public string? ShippingMethodName { get; init; }
    public string? ShippingEstimatedDelivery { get; init; }
    public string SubtotalDisplay { get; init; } = string.Empty;
    public string ShippingAmountDisplay { get; init; } = string.Empty;
    public string DiscountAmountDisplay { get; init; } = string.Empty;
    public string? VatAmountDisplay { get; init; }
    public decimal? VatPercent { get; init; }
    public string? CouponCode { get; init; }
    public string TotalDisplay { get; init; } = string.Empty;
    public string? PaymentProvider { get; init; }
    public string? PaymentTransactionId { get; init; }
    public string? Notes { get; init; }
    public string? AdminNotes { get; init; }
    public bool IsWholesale { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? PaymentExpiresAtUtc { get; init; }
    public IReadOnlyList<OrderLineViewModel> Lines { get; init; } = [];
    public IEnumerable<SelectListItem> StatusOptions { get; set; } = [];
}

public sealed class OrderLineViewModel
{
    public string ProductTitle { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public string UnitPriceDisplay { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public string LineTotalDisplay { get; init; } = string.Empty;
}

public sealed class OrderShipmentPrintViewModel
{
    public string? LogoUrl { get; init; }
    public string? SiteName { get; init; }
    public string? SellerName { get; init; }
    public string? SellerPhone { get; init; }
    public string? SellerProvince { get; init; }
    public string? SellerCity { get; init; }
    public string? SellerAddress { get; init; }
    public string? SellerPostalCode { get; init; }
    public string RecipientName { get; init; } = string.Empty;
    public string? RecipientPhone { get; init; }
    public string? ShippingCity { get; init; }
    public string? ShippingAddress { get; init; }
    public string? RecipientPostalCode { get; init; }
}

public sealed class OrderInvoicePrintViewModel
{
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string Currency { get; init; } = "IRR";
    public string? LogoUrl { get; init; }
    public string? SiteName { get; init; }
    public string StatusDisplay { get; init; } = string.Empty;
    public string PaymentStatusDisplay { get; init; } = string.Empty;
    public string? PaymentMethodDisplay { get; init; }
    public string? PaymentTransactionId { get; init; }
    public string? PaymentProvider { get; init; }
    public string? ShippingMethodName { get; init; }
    public string? CouponCode { get; init; }
    public string? Notes { get; init; }
    public bool IsWholesale { get; init; }
    public string? SellerName { get; init; }
    public string? SellerPhone { get; init; }
    public string? SellerEmail { get; init; }
    public string? SellerProvince { get; init; }
    public string? SellerCity { get; init; }
    public string? SellerAddress { get; init; }
    public string? SellerPostalCode { get; init; }
    public string? SellerNationalId { get; init; }
    public string? SellerEconomicCode { get; init; }
    public string? SellerRegistrationNumber { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string? RecipientName { get; init; }
    public string? RecipientPhone { get; init; }
    public string? ShippingCity { get; init; }
    public string? ShippingAddress { get; init; }
    public string SubtotalDisplay { get; init; } = string.Empty;
    public string ShippingAmountDisplay { get; init; } = string.Empty;
    public string DiscountAmountDisplay { get; init; } = string.Empty;
    public string? VatAmountDisplay { get; init; }
    public decimal? VatPercent { get; init; }
    public string TotalDisplay { get; init; } = string.Empty;
    public decimal SubtotalAmount { get; init; }
    public decimal ShippingAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal VatAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public IReadOnlyList<OrderInvoiceLineViewModel> Lines { get; init; } = [];
    public IReadOnlyList<InvoicePartyFieldViewModel> SellerFields { get; init; } = [];
    public IReadOnlyList<InvoicePartyFieldViewModel> BuyerFields { get; init; } = [];
}

public sealed class InvoicePartyFieldViewModel
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public bool IsLtr { get; init; }
}

public sealed class OrderInvoiceLineViewModel
{
    public int RowNumber { get; init; }
    public string ProductTitle { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public int Quantity { get; init; }
    public string UnitPriceDisplay { get; init; } = string.Empty;
    public string LineTotalDisplay { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}
