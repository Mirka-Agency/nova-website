using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Domain.Commerce;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Domain.Entities;

public class Order : BaseEntity
{
    private readonly List<OrderLine> _lines = [];

    private Order()
    {
    }

    public string OrderNumber { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; private set; } = PaymentStatus.Unpaid;
    public PaymentMethod? PaymentMethod { get; private set; }
    public string? UserId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public string? CustomerPhone { get; private set; }
    public string? RecipientName { get; private set; }
    public string? RecipientPhone { get; private set; }
    public string? ShippingAddress { get; private set; }
    public string? ShippingCity { get; private set; }
    public Guid? ShippingMethodId { get; private set; }
    public string? ShippingMethodName { get; private set; }
    public string? ShippingEstimatedDelivery { get; private set; }
    public decimal ShippingAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal? VatPercent { get; private set; }
    public string? CouponCode { get; private set; }
    public string Currency { get; private set; } = "IRR";
    public decimal SubtotalAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? PaymentProvider { get; private set; }
    public string? PaymentTransactionId { get; private set; }
    public string? Notes { get; private set; }
    public string? AdminNotes { get; private set; }
    public bool IsWholesale { get; private set; }
    /// <summary>UTC deadline for unpaid Pending payment; null when paid/cancelled/not applicable.</summary>
    public DateTime? PaymentExpiresAtUtc { get; private set; }
    /// <summary>When the abandoned-payment reminder SMS was sent.</summary>
    public DateTime? PaymentReminderSentAtUtc { get; private set; }
    /// <summary>True after reserved stock/coupon was restored (cancel/expire/payment fail). Prevents double-release.</summary>
    public bool StockReservationReleased { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines;

    public bool IsUnpaidPending =>
        Status == OrderStatus.Pending && PaymentStatus == PaymentStatus.Unpaid && !StockReservationReleased;

    public bool IsPaymentExpired(DateTime utcNow) =>
        IsUnpaidPending
        && PaymentExpiresAtUtc.HasValue
        && PaymentExpiresAtUtc.Value <= utcNow;

    public static Order Create(
        string orderNumber,
        string? userId,
        string customerName,
        string customerEmail,
        string? customerPhone,
        string? recipientName,
        string? recipientPhone,
        string? shippingAddress,
        string? shippingCity,
        string currency,
        string? notes,
        bool isWholesale)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new DomainException("شماره سفارش الزامی است.");
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("نام مشتری الزامی است.");
        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new DomainException("ایمیل مشتری الزامی است.");
        if (string.IsNullOrWhiteSpace(recipientName))
            throw new DomainException("نام تحویل‌گیرنده الزامی است.");
        if (string.IsNullOrWhiteSpace(recipientPhone))
            throw new DomainException("موبایل تحویل‌گیرنده الزامی است.");
        if (string.IsNullOrWhiteSpace(shippingAddress))
            throw new DomainException("آدرس ارسال الزامی است.");

        return new Order
        {
            OrderNumber = orderNumber.Trim().ToUpperInvariant(),
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim(),
            CustomerName = customerName.Trim(),
            CustomerEmail = customerEmail.Trim(),
            CustomerPhone = string.IsNullOrWhiteSpace(customerPhone) ? null : customerPhone.Trim(),
            RecipientName = recipientName.Trim(),
            RecipientPhone = recipientPhone.Trim(),
            ShippingAddress = shippingAddress.Trim(),
            ShippingCity = string.IsNullOrWhiteSpace(shippingCity) ? null : shippingCity.Trim(),
            Currency = string.IsNullOrWhiteSpace(currency) ? "IRR" : currency.Trim().ToUpperInvariant(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Unpaid,
            IsWholesale = isWholesale
        };
    }

    public OrderLine AddLine(
        Guid productId,
        Guid? variationId,
        string productTitle,
        string? sku,
        decimal unitPrice,
        int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("تعداد باید مثبت باشد.");
        if (unitPrice < 0)
            throw new DomainException("قیمت واحد نمی‌تواند منفی باشد.");

        var line = OrderLine.Create(Id, productId, variationId, productTitle, sku, unitPrice, quantity);
        _lines.Add(line);
        RecalculateTotals();
        return line;
    }

    public void SetShipping(Guid? methodId, string? methodName, decimal amount, string? estimatedDelivery = null)
    {
        if (amount < 0)
            throw new DomainException("هزینه ارسال نمی‌تواند منفی باشد.");
        ShippingMethodId = methodId;
        ShippingMethodName = string.IsNullOrWhiteSpace(methodName) ? null : methodName.Trim();
        ShippingEstimatedDelivery = string.IsNullOrWhiteSpace(estimatedDelivery) ? null : estimatedDelivery.Trim();
        ShippingAmount = amount;
        RecalculateTotals();
    }

    public void SetDiscount(decimal amount, string? couponCode)
    {
        if (amount < 0)
            throw new DomainException("تخفیف نمی‌تواند منفی باشد.");
        DiscountAmount = amount;
        CouponCode = string.IsNullOrWhiteSpace(couponCode) ? null : couponCode.Trim().ToUpperInvariant();
        RecalculateTotals();
    }

    public void SetVat(decimal? percent)
    {
        if (percent.HasValue && percent.Value is < 0 or > 100)
            throw new DomainException("درصد مالیات بر ارزش افزوده نامعتبر است.");

        VatPercent = percent is > 0 ? percent : null;
        RecalculateTotals();
    }

    public void SetPaymentMethod(PaymentMethod method)
    {
        if (!Enum.IsDefined(method))
            throw new DomainException("روش پرداخت نامعتبر است.");
        PaymentMethod = method;
        Touch();
    }

    public void SetPaymentDeadline(DateTime expiresAtUtc)
    {
        if (expiresAtUtc.Kind != DateTimeKind.Utc)
            expiresAtUtc = DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc);
        PaymentExpiresAtUtc = expiresAtUtc;
        Touch();
    }

    public void ClearPaymentDeadline()
    {
        PaymentExpiresAtUtc = null;
        Touch();
    }

    public void MarkPaymentReminderSent(DateTime sentAtUtc)
    {
        if (sentAtUtc.Kind != DateTimeKind.Utc)
            sentAtUtc = DateTime.SpecifyKind(sentAtUtc, DateTimeKind.Utc);
        PaymentReminderSentAtUtc = sentAtUtc;
        Touch();
    }

    public void MarkPaid(string provider, string? transactionId, bool allowExpiredDeadline = false)
    {
        if (Status == OrderStatus.Cancelled || StockReservationReleased)
            throw new DomainException("سفارش لغوشده قابل پرداخت نیست.");
        if (!allowExpiredDeadline
            && PaymentExpiresAtUtc.HasValue
            && PaymentExpiresAtUtc.Value <= DateTime.UtcNow)
            throw new DomainException("مهلت پرداخت این سفارش به پایان رسیده است.");

        PaymentProvider = string.IsNullOrWhiteSpace(provider) ? null : provider.Trim();
        PaymentTransactionId = string.IsNullOrWhiteSpace(transactionId) ? null : transactionId.Trim();
        PaymentStatus = PaymentStatus.Paid;
        PaymentExpiresAtUtc = null;
        if (Status == OrderStatus.Pending)
            Status = OrderStatus.Processing;
        Touch();
    }

    public void MarkInvoiceRequested()
    {
        PaymentStatus = PaymentStatus.InvoiceRequested;
        Status = OrderStatus.Reviewing;
        PaymentExpiresAtUtc = null;
        Touch();
    }

    public void CancelUnpaidReservation()
    {
        if (!IsUnpaidPending)
            throw new DomainException("فقط سفارش در انتظار پرداخت قابل لغو است.");

        Status = OrderStatus.Cancelled;
        PaymentExpiresAtUtc = null;
        Touch();
    }

    public void MarkStockReservationReleased()
    {
        StockReservationReleased = true;
        Touch();
    }

    public void SetAdminNotes(string? notes)
    {
        AdminNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Touch();
    }

    public void ChangeStatus(OrderStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new DomainException("وضعیت سفارش نامعتبر است.");
        if (_lines.Count == 0)
            throw new DomainException("سفارش هیچ آیتمی ندارد.");

        Status = status;
        if (status == OrderStatus.Cancelled)
            PaymentExpiresAtUtc = null;
        Touch();
    }

    private void RecalculateTotals()
    {
        SubtotalAmount = _lines.Sum(l => l.LineTotal);
        var baseAmount = Math.Max(0, SubtotalAmount - DiscountAmount + ShippingAmount);
        VatAmount = VatPercent.HasValue
            ? VatCalculator.ComputeAmount(baseAmount, VatPercent.Value)
            : 0;
        TotalAmount = baseAmount + VatAmount;
        Touch();
    }
}

public class OrderLine : BaseEntity
{
    private OrderLine()
    {
    }

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public Guid? VariationId { get; private set; }
    public string ProductTitle { get; private set; } = string.Empty;
    public string? Sku { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;

    public static OrderLine Create(
        Guid orderId,
        Guid productId,
        Guid? variationId,
        string productTitle,
        string? sku,
        decimal unitPrice,
        int quantity)
    {
        return new OrderLine
        {
            OrderId = orderId,
            ProductId = productId,
            VariationId = variationId,
            ProductTitle = productTitle.Trim(),
            Sku = string.IsNullOrWhiteSpace(sku) ? null : sku.Trim(),
            UnitPrice = unitPrice,
            Quantity = quantity
        };
    }
}
