using CMS.Modules.Shop.Application.Payments;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application.Interfaces;

public interface IPaymentConfigService
{
    Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentProviderConfigDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentProviderConfigDto>> ListEnabledAsync(CancellationToken cancellationToken = default);
    Task<PaymentProviderConfigDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentProviderConfigDto?> GetByTypeAsync(PaymentProviderType type, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SavePaymentProviderCommand command, CancellationToken cancellationToken = default);
}

public interface IPaymentOrchestrator
{
    Task<PaymentInitiationResponse> InitiateForOrderAsync(
        Guid orderId,
        Guid paymentProviderConfigId,
        string callbackBaseUrl,
        CancellationToken cancellationToken = default);

    Task<PaymentVerificationResponse> VerifyCallbackAsync(
        PaymentCallbackContext context,
        string callbackBaseUrl,
        CancellationToken cancellationToken = default);

    Task<PaymentVerificationResponse> CompleteReturnAsync(
        Guid orderId,
        PaymentProviderType providerType,
        IReadOnlyDictionary<string, string> queryParameters,
        string callbackBaseUrl,
        CancellationToken cancellationToken = default);
}
