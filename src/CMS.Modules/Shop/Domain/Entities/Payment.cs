using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Domain.Entities;

public class PaymentProviderConfig : BaseEntity
{
    private PaymentProviderConfig()
    {
    }

    public PaymentProviderType ProviderType { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; }
    public bool IsSandbox { get; private set; }
    public int SortOrder { get; private set; }
    public string EncryptedSettings { get; private set; } = string.Empty;

    public static PaymentProviderConfig Create(
        PaymentProviderType providerType,
        string displayName,
        bool isSandbox,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("نام درگاه الزامی است.");

        return new PaymentProviderConfig
        {
            ProviderType = providerType,
            DisplayName = displayName.Trim(),
            IsSandbox = isSandbox,
            SortOrder = Math.Max(0, sortOrder),
            IsEnabled = false,
            EncryptedSettings = string.Empty
        };
    }

    public void Update(string displayName, bool isEnabled, bool isSandbox, int sortOrder, string encryptedSettings)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("نام درگاه الزامی است.");
        if (string.IsNullOrWhiteSpace(encryptedSettings))
            throw new DomainException("تنظیمات درگاه الزامی است.");

        DisplayName = displayName.Trim();
        IsEnabled = isEnabled;
        IsSandbox = isSandbox;
        SortOrder = Math.Max(0, sortOrder);
        EncryptedSettings = encryptedSettings;
        Touch();
    }
}

public class PaymentAttempt : BaseEntity
{
    private PaymentAttempt()
    {
    }

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public Guid PaymentProviderConfigId { get; private set; }
    public PaymentProviderType ProviderType { get; private set; }
    public string? ReferenceId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "IRR";
    public PaymentAttemptStatus Status { get; private set; } = PaymentAttemptStatus.Pending;
    public string? CallbackPayload { get; private set; }
    public string? TransactionId { get; private set; }
    public string? FailureMessage { get; private set; }

    public static PaymentAttempt Create(
        Guid orderId,
        Guid paymentProviderConfigId,
        PaymentProviderType providerType,
        decimal amount,
        string currency)
    {
        if (amount < 0)
            throw new DomainException("مبلغ پرداخت نامعتبر است.");

        return new PaymentAttempt
        {
            OrderId = orderId,
            PaymentProviderConfigId = paymentProviderConfigId,
            ProviderType = providerType,
            Amount = amount,
            Currency = string.IsNullOrWhiteSpace(currency) ? "IRR" : currency.Trim().ToUpperInvariant(),
            Status = PaymentAttemptStatus.Pending
        };
    }

    public void MarkRedirected(string referenceId)
    {
        if (string.IsNullOrWhiteSpace(referenceId))
            throw new DomainException("شناسه پرداخت نامعتبر است.");

        ReferenceId = referenceId.Trim();
        Status = PaymentAttemptStatus.Redirected;
        Touch();
    }

    public void MarkVerified(string transactionId, string? callbackPayload)
    {
        TransactionId = string.IsNullOrWhiteSpace(transactionId) ? null : transactionId.Trim();
        CallbackPayload = callbackPayload;
        Status = PaymentAttemptStatus.Verified;
        Touch();
    }

    public void MarkFailed(string? message, string? callbackPayload = null)
    {
        FailureMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        CallbackPayload = callbackPayload ?? CallbackPayload;
        Status = PaymentAttemptStatus.Failed;
        Touch();
    }
}
