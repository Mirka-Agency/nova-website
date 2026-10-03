using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Domain.Entities;

public class ShopSettings : BaseEntity
{
    public const int DefaultPendingPaymentTimeoutMinutes = 30;
    public const int DefaultAbandonedPaymentReminderMinutes = 3;
    public const decimal DefaultVatPercent = 10m;
    public const InvoiceSellerDisplayFields DefaultInvoiceSellerDisplayFields =
        InvoiceSellerDisplayFields.Name | InvoiceSellerDisplayFields.Phone | InvoiceSellerDisplayFields.Address;
    public const InvoiceBuyerDisplayFields DefaultInvoiceBuyerDisplayFields =
        InvoiceBuyerDisplayFields.CustomerName
        | InvoiceBuyerDisplayFields.RecipientName
        | InvoiceBuyerDisplayFields.RecipientPhone
        | InvoiceBuyerDisplayFields.ShippingAddress;

    private ShopSettings()
    {
    }

    public ShopMode Mode { get; private set; } = ShopMode.CatalogOnly;
    public SalesAudience SalesAudience { get; private set; } = SalesAudience.RetailOnly;
    public bool EnableOnlinePayment { get; private set; } = true;
    public bool EnableBankTransfer { get; private set; } = true;
    public string? BankAccountHolderName { get; private set; }
    public string? BankName { get; private set; }
    public string? BankCardNumber { get; private set; }
    public string? BankShebaNumber { get; private set; }
    public string? BankTransferInstructions { get; private set; }
    public bool ShowPricesToGuests { get; private set; } = true;
    public bool EnableReviews { get; private set; } = true;
    public bool EnableCoupons { get; private set; } = true;
    public bool EnableWholesaleInvoice { get; private set; } = true;
    public decimal? WholesaleMinimumOrderAmount { get; private set; }
    public int? WholesaleMinimumOrderQuantity { get; private set; }
    /// <summary>Minutes a Pending unpaid order may hold reserved stock before auto-cancel.</summary>
    public int PendingPaymentTimeoutMinutes { get; private set; } = DefaultPendingPaymentTimeoutMinutes;
    /// <summary>Minutes after order creation before sending abandoned-payment reminder SMS.</summary>
    public int AbandonedPaymentReminderMinutes { get; private set; } = DefaultAbandonedPaymentReminderMinutes;
    public bool EnableVat { get; private set; }
    public decimal VatPercent { get; private set; } = DefaultVatPercent;
    public string? SellerName { get; private set; }
    public string? SellerPhone { get; private set; }
    public string? SellerProvince { get; private set; }
    public string? SellerCity { get; private set; }
    public string? SellerAddress { get; private set; }
    public string? SellerPostalCode { get; private set; }
    public string? SellerEmail { get; private set; }
    public string? SellerNationalId { get; private set; }
    public string? SellerEconomicCode { get; private set; }
    public string? SellerRegistrationNumber { get; private set; }
    public InvoiceSellerDisplayFields InvoiceSellerDisplayFields { get; private set; } = DefaultInvoiceSellerDisplayFields;
    public InvoiceBuyerDisplayFields InvoiceBuyerDisplayFields { get; private set; } = DefaultInvoiceBuyerDisplayFields;

    public bool HasBankTransferAccount =>
        !string.IsNullOrWhiteSpace(BankCardNumber) || !string.IsNullOrWhiteSpace(BankShebaNumber);

    public static ShopSettings CreateDefault() => new()
    {
        Mode = ShopMode.CatalogOnly,
        SalesAudience = SalesAudience.RetailOnly,
        PendingPaymentTimeoutMinutes = DefaultPendingPaymentTimeoutMinutes,
        AbandonedPaymentReminderMinutes = DefaultAbandonedPaymentReminderMinutes,
        VatPercent = DefaultVatPercent
    };

    public void SetMode(ShopMode mode)
    {
        if (!Enum.IsDefined(mode))
            throw new DomainException("حالت فروشگاه نامعتبر است.");

        Mode = mode;
        Touch();
    }

    public void UpdateCommerceOptions(
        SalesAudience salesAudience,
        bool enableOnlinePayment,
        bool enableBankTransfer,
        string? bankAccountHolderName,
        string? bankName,
        string? bankCardNumber,
        string? bankShebaNumber,
        string? bankTransferInstructions,
        bool showPricesToGuests,
        bool enableReviews,
        bool enableCoupons,
        bool enableWholesaleInvoice,
        decimal? wholesaleMinimumOrderAmount,
        int? wholesaleMinimumOrderQuantity,
        int pendingPaymentTimeoutMinutes,
        int abandonedPaymentReminderMinutes,
        bool enableVat,
        decimal vatPercent)
    {
        if (!Enum.IsDefined(salesAudience))
            throw new DomainException("حالت فروش نامعتبر است.");
        if (pendingPaymentTimeoutMinutes is < 1 or > 24 * 60)
            throw new DomainException("مهلت پرداخت باید بین ۱ تا ۱۴۴۰ دقیقه باشد.");
        if (abandonedPaymentReminderMinutes is < 1 or > 24 * 60)
            throw new DomainException("زمان یادآوری پرداخت باید بین ۱ تا ۱۴۴۰ دقیقه باشد.");
        if (abandonedPaymentReminderMinutes >= pendingPaymentTimeoutMinutes)
            throw new DomainException("زمان یادآوری باید کمتر از مهلت پرداخت باشد.");
        if (vatPercent is < 0 or > 100)
            throw new DomainException("درصد مالیات بر ارزش افزوده باید بین ۰ تا ۱۰۰ باشد.");

        var holder = NormalizeOptional(bankAccountHolderName, 200);
        var bank = NormalizeOptional(bankName, 100);
        var card = NormalizeCardNumber(bankCardNumber);
        var sheba = NormalizeSheba(bankShebaNumber);
        var instructions = NormalizeOptional(bankTransferInstructions, 2000);

        if (enableBankTransfer)
        {
            if (string.IsNullOrWhiteSpace(holder))
                throw new DomainException("نام صاحب حساب برای واریز بانکی الزامی است.");
            if (string.IsNullOrWhiteSpace(card) && string.IsNullOrWhiteSpace(sheba))
                throw new DomainException("حداقل شماره کارت یا شبا برای واریز بانکی الزامی است.");
        }

        SalesAudience = salesAudience;
        EnableOnlinePayment = enableOnlinePayment;
        EnableBankTransfer = enableBankTransfer;
        BankAccountHolderName = holder;
        BankName = bank;
        BankCardNumber = card;
        BankShebaNumber = sheba;
        BankTransferInstructions = instructions;
        ShowPricesToGuests = showPricesToGuests;
        EnableReviews = enableReviews;
        EnableCoupons = enableCoupons;
        EnableWholesaleInvoice = enableWholesaleInvoice;
        WholesaleMinimumOrderAmount = wholesaleMinimumOrderAmount;
        WholesaleMinimumOrderQuantity = wholesaleMinimumOrderQuantity;
        PendingPaymentTimeoutMinutes = pendingPaymentTimeoutMinutes;
        AbandonedPaymentReminderMinutes = abandonedPaymentReminderMinutes;
        EnableVat = enableVat;
        VatPercent = vatPercent;
        Touch();
    }

    public void UpdateSellerInfo(
        string? name,
        string? phone,
        string? email,
        string? province,
        string? city,
        string? address,
        string? postalCode,
        string? nationalId,
        string? economicCode,
        string? registrationNumber,
        InvoiceSellerDisplayFields invoiceSellerDisplayFields,
        InvoiceBuyerDisplayFields invoiceBuyerDisplayFields)
    {
        SellerName = NormalizeOptional(name, 200);
        SellerPhone = NormalizeOptional(phone, 40);
        SellerEmail = NormalizeOptional(email, 256);
        SellerProvince = NormalizeOptional(province, 100);
        SellerCity = NormalizeOptional(city, 100);
        SellerAddress = NormalizeOptional(address, 500);
        SellerPostalCode = NormalizePostalCode(postalCode);
        SellerNationalId = NormalizeDigitsOptional(nationalId, 11, "شناسه ملی");
        SellerEconomicCode = NormalizeDigitsOptional(economicCode, 14, "کد اقتصادی");
        SellerRegistrationNumber = NormalizeOptional(registrationNumber, 50);
        InvoiceSellerDisplayFields = invoiceSellerDisplayFields;
        InvoiceBuyerDisplayFields = invoiceBuyerDisplayFields;
        Touch();
    }

    public bool IsCatalogOnly => Mode == ShopMode.CatalogOnly;
    public bool IsOnlineStore => Mode == ShopMode.OnlineStore;
    public bool IsHybrid => Mode == ShopMode.Hybrid;
    public bool EcommerceEnabled => Mode is ShopMode.OnlineStore or ShopMode.Hybrid;

    public bool CanPurchaseProduct(bool productIsPurchasable) =>
        Mode switch
        {
            ShopMode.CatalogOnly => false,
            ShopMode.OnlineStore => true,
            ShopMode.Hybrid => productIsPurchasable,
            _ => false
        };

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"طول متن نباید بیشتر از {maxLength} کاراکتر باشد.");
        return trimmed;
    }

    private static string? NormalizeCardNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length is < 16 or > 19)
            throw new DomainException("شماره کارت باید بین ۱۶ تا ۱۹ رقم باشد.");
        return digits;
    }

    private static string? NormalizeSheba(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Replace(" ", "", StringComparison.Ordinal).Trim().ToUpperInvariant();
        if (normalized.StartsWith("IR", StringComparison.Ordinal) && normalized.Length == 26
            && normalized.Skip(2).All(char.IsDigit))
            return normalized;

        if (normalized.Length == 24 && normalized.All(char.IsDigit))
            return "IR" + normalized;

        throw new DomainException("شماره شبا نامعتبر است (مثال: IR120170000000123456789001).");
    }

    private static string? NormalizePostalCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length is < 5 or > 10)
            throw new DomainException("کد پستی باید بین ۵ تا ۱۰ رقم باشد.");
        return digits;
    }

    private static string? NormalizeDigitsOptional(string? value, int maxLength, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length > maxLength)
            throw new DomainException($"{fieldLabel} نباید بیشتر از {maxLength} رقم باشد.");
        return digits;
    }
}
