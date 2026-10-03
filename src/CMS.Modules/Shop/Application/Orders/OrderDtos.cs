using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Application.Settings;

namespace CMS.Modules.Shop.Application.Orders;

public sealed record OrderListItemDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string CustomerEmail,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal TotalAmount,
    string Currency,
    bool IsWholesale,
    DateTime CreatedAtUtc,
    DateTime? PaymentExpiresAtUtc,
    bool CanCancel);

public sealed record OrderLineDto(
    Guid ProductId,
    Guid? VariationId,
    string ProductTitle,
    string? Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record OrderDetailDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    PaymentMethod? PaymentMethod,
    string? UserId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string? RecipientName,
    string? RecipientPhone,
    string? ShippingAddress,
    string? ShippingCity,
    string? ShippingMethodName,
    string? ShippingEstimatedDelivery,
    decimal SubtotalAmount,
    decimal ShippingAmount,
    decimal DiscountAmount,
    string? CouponCode,
    decimal VatAmount,
    decimal? VatPercent,
    string Currency,
    decimal TotalAmount,
    string? PaymentProvider,
    string? PaymentTransactionId,
    string? Notes,
    string? AdminNotes,
    bool IsWholesale,
    DateTime CreatedAtUtc,
    DateTime? PaymentExpiresAtUtc,
    bool CanCancel,
    IReadOnlyList<OrderLineDto> Lines);

public sealed record CheckoutCommand(
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    Guid AddressId,
    string? Notes,
    PaymentMethod PaymentMethod,
    Guid? ShippingMethodId,
    Guid? PaymentProviderConfigId,
    string UserId,
    string CallbackBaseUrl);

public sealed record CheckoutResultDto(
    Guid OrderId,
    string OrderNumber,
    bool PaymentSucceeded,
    string? PaymentMessage,
    string? TransactionId,
    PaymentStatus PaymentStatus,
    string? RedirectUrl,
    bool AwaitingOfflinePayment = false,
    BankTransferDetailsDto? BankTransfer = null);

public sealed record ChangeOrderStatusCommand(OrderStatus Status);
public sealed record UpdateOrderAdminNotesCommand(string? AdminNotes);
public sealed record ConfirmBankTransferPaidCommand(string? TransactionReference);
