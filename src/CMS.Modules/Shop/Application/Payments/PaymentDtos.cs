using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application.Payments;

public sealed record PaymentProviderConfigDto(
    Guid Id,
    PaymentProviderType ProviderType,
    string DisplayName,
    bool IsEnabled,
    bool IsSandbox,
    int SortOrder,
    PaymentProviderSettingsDto Settings);

public sealed record PaymentProviderSettingsDto
{
    public string? MerchantId { get; init; }
    public string? TerminalId { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
}

public sealed record SavePaymentProviderCommand(
    string DisplayName,
    bool IsEnabled,
    bool IsSandbox,
    int SortOrder,
    PaymentProviderSettingsDto Settings);

public sealed record PaymentInitiationResponse(
    Guid OrderId,
    string OrderNumber,
    string? RedirectUrl,
    string? Message);

public sealed record PaymentVerificationResponse(
    Guid OrderId,
    string OrderNumber,
    bool Succeeded,
    string? Message,
    string? TransactionId);

public sealed record PaymentCallbackContext(
    PaymentProviderType ProviderType,
    Guid? OrderId,
    IReadOnlyDictionary<string, string> Parameters);
