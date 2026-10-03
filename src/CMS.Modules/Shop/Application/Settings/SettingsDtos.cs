using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application.Settings;

public sealed record ShopSettingsDto(
    ShopMode Mode,
    SalesAudience SalesAudience,
    bool IsCatalogOnly,
    bool IsOnlineStore,
    bool IsHybrid,
    bool EcommerceEnabled,
    bool EnableOnlinePayment,
    bool EnableBankTransfer,
    string? BankAccountHolderName,
    string? BankName,
    string? BankCardNumber,
    string? BankShebaNumber,
    string? BankTransferInstructions,
    bool ShowPricesToGuests,
    bool EnableReviews,
    bool EnableCoupons,
    bool EnableWholesaleInvoice,
    decimal? WholesaleMinimumOrderAmount,
    int? WholesaleMinimumOrderQuantity,
    int PendingPaymentTimeoutMinutes,
    int AbandonedPaymentReminderMinutes,
    bool EnableVat,
    decimal VatPercent,
    string? SellerName,
    string? SellerPhone,
    string? SellerProvince,
    string? SellerCity,
    string? SellerAddress,
    string? SellerPostalCode,
    string? SellerEmail,
    string? SellerNationalId,
    string? SellerEconomicCode,
    string? SellerRegistrationNumber,
    InvoiceSellerDisplayFields InvoiceSellerDisplayFields,
    InvoiceBuyerDisplayFields InvoiceBuyerDisplayFields)
{
    public bool HasBankTransferAccount =>
        !string.IsNullOrWhiteSpace(BankCardNumber) || !string.IsNullOrWhiteSpace(BankShebaNumber);
}

public sealed record UpdateShopModeCommand(ShopMode Mode);

public sealed record UpdateShopCommerceSettingsCommand(
    SalesAudience SalesAudience,
    bool EnableOnlinePayment,
    bool EnableBankTransfer,
    string? BankAccountHolderName,
    string? BankName,
    string? BankCardNumber,
    string? BankShebaNumber,
    string? BankTransferInstructions,
    bool ShowPricesToGuests,
    bool EnableReviews,
    bool EnableCoupons,
    bool EnableWholesaleInvoice,
    decimal? WholesaleMinimumOrderAmount,
    int? WholesaleMinimumOrderQuantity,
    int PendingPaymentTimeoutMinutes,
    int AbandonedPaymentReminderMinutes,
    bool EnableVat,
    decimal VatPercent);

public sealed record UpdateShopSellerSettingsCommand(
    string? SellerName,
    string? SellerPhone,
    string? SellerEmail,
    string? SellerProvince,
    string? SellerCity,
    string? SellerAddress,
    string? SellerPostalCode,
    string? SellerNationalId,
    string? SellerEconomicCode,
    string? SellerRegistrationNumber,
    InvoiceSellerDisplayFields InvoiceSellerDisplayFields,
    InvoiceBuyerDisplayFields InvoiceBuyerDisplayFields);

public sealed record BankTransferDetailsDto(
    string? AccountHolderName,
    string? BankName,
    string? CardNumber,
    string? ShebaNumber,
    string? Instructions);
