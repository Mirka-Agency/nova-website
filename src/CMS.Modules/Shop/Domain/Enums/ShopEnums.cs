namespace CMS.Modules.Shop.Domain.Enums;

public enum ProductStatus
{
    Draft = 0,
    Active = 1,
    Hidden = 2,
    OutOfStock = 3
}

public enum ProductType
{
    Simple = 0,
    Variable = 1
}

public enum VariationStatus
{
    Active = 0,
    Hidden = 1,
    OutOfStock = 2
}

public enum ShopMode
{
    CatalogOnly = 0,
    OnlineStore = 1,
    Hybrid = 2
}

public enum SalesAudience
{
    RetailOnly = 0,
    WholesaleOnly = 1,
    Both = 2
}

public enum OrderStatus
{
    Pending = 0,
    Reviewing = 1,
    Approved = 2,
    Processing = 3,
    ReadyForShipping = 4,
    Shipped = 5,
    Completed = 6,
    Cancelled = 7
}

public enum PaymentStatus
{
    Unpaid = 0,
    Paid = 1,
    InvoiceRequested = 2
}

public enum PaymentMethod
{
    Online = 0,
    BankTransfer = 1,
    CashOnDelivery = 2,
    Invoice = 3,
    PayLater = 4,
    SnapPay = 5
}

public enum StockStatus
{
    Available = 0,
    LowStock = 1,
    OutOfStock = 2
}

public enum PriceRuleType
{
    RetailSale = 0,
    CustomerGroup = 1,
    QuantityTier = 2
}

public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1,
    Product = 2,
    Category = 3,
    Quantity = 4
}

public enum ShippingCalculationType
{
    Fixed = 0,
    WeightBased = 1,
    CityBased = 2
}

public enum WholesaleRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum PaymentProviderType
{
    Zarinpal = 0,
    Zibal = 1,
    Sep = 2,
    Mellat = 3,
    SnapPay = 4
}

public enum PaymentAttemptStatus
{
    Pending = 0,
    Redirected = 1,
    Verified = 2,
    Failed = 3
}

[Flags]
public enum InvoiceSellerDisplayFields
{
    None = 0,
    Name = 1 << 0,
    NationalId = 1 << 1,
    EconomicCode = 1 << 2,
    RegistrationNumber = 1 << 3,
    Phone = 1 << 4,
    Email = 1 << 5,
    City = 1 << 6,
    PostalCode = 1 << 7,
    Address = 1 << 8
}

[Flags]
public enum InvoiceBuyerDisplayFields
{
    None = 0,
    CustomerName = 1 << 0,
    CustomerEmail = 1 << 1,
    CustomerPhone = 1 << 2,
    RecipientName = 1 << 3,
    RecipientPhone = 1 << 4,
    ShippingCity = 1 << 5,
    ShippingAddress = 1 << 6,
    IsWholesale = 1 << 7
}
