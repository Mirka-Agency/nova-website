namespace CMS.Application.Payments;

public enum PaymentProviderKind
{
    Zarinpal = 0,
    Zibal = 1,
    Sep = 2,
    Mellat = 3,
    SnapPay = 4
}

public sealed record PaymentInitiationRequest(
    Guid AttemptId,
    string OrderNumber,
    decimal Amount,
    string Currency,
    string? CustomerEmail,
    string? CustomerPhone,
    string? Description,
    string CallbackUrl,
    string ReturnUrl);

public sealed record PaymentInitiationResult(
    bool Succeeded,
    string? RedirectUrl,
    string? ReferenceId,
    string? Message,
    string ProviderDisplayName);

public sealed record PaymentVerificationRequest(
    Guid AttemptId,
    string OrderNumber,
    decimal Amount,
    string Currency,
    string? ReferenceId,
    IReadOnlyDictionary<string, string> CallbackParameters);

public sealed record PaymentVerificationResult(
    bool Succeeded,
    string? TransactionId,
    string? Message,
    string ProviderDisplayName);

public sealed record PaymentProviderRuntimeSettings(
    PaymentProviderKind Kind,
    bool IsSandbox,
    IReadOnlyDictionary<string, string> Values);

public interface IPaymentProvider
{
    PaymentProviderKind Kind { get; }
    Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        PaymentProviderRuntimeSettings settings,
        CancellationToken cancellationToken = default);
    Task<PaymentVerificationResult> VerifyAsync(
        PaymentVerificationRequest request,
        PaymentProviderRuntimeSettings settings,
        CancellationToken cancellationToken = default);
}

public interface IPaymentProviderFactory
{
    IPaymentProvider Get(PaymentProviderKind kind);
    IReadOnlyCollection<IPaymentProvider> GetAll();
}

public interface IPaymentSettingsProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedText);
}
