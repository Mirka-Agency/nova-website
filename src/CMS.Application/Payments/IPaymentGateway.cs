namespace CMS.Application.Payments;

public sealed record PaymentRequest(
    string OrderNumber,
    decimal Amount,
    string Currency,
    string? CustomerEmail,
    string? Description,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record PaymentResult(
    bool Succeeded,
    string? TransactionId,
    string? Message,
    string Provider);

public interface IPaymentGateway
{
    string ProviderName { get; }
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}
