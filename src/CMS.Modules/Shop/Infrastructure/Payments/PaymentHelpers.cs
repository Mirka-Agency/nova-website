using System.Text.Json;
using CMS.Application.Payments;
using CMS.Modules.Shop.Application.Payments;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Infrastructure.Payments;

internal static class PaymentProviderKindMapper
{
    public static PaymentProviderKind ToKind(PaymentProviderType type) => type switch
    {
        PaymentProviderType.Zarinpal => PaymentProviderKind.Zarinpal,
        PaymentProviderType.Zibal => PaymentProviderKind.Zibal,
        PaymentProviderType.Sep => PaymentProviderKind.Sep,
        PaymentProviderType.Mellat => PaymentProviderKind.Mellat,
        PaymentProviderType.SnapPay => PaymentProviderKind.SnapPay,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static PaymentProviderType ToType(PaymentProviderKind kind) => kind switch
    {
        PaymentProviderKind.Zarinpal => PaymentProviderType.Zarinpal,
        PaymentProviderKind.Zibal => PaymentProviderType.Zibal,
        PaymentProviderKind.Sep => PaymentProviderType.Sep,
        PaymentProviderKind.Mellat => PaymentProviderType.Mellat,
        PaymentProviderKind.SnapPay => PaymentProviderType.SnapPay,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}

internal static class PaymentSettingsSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string Serialize(PaymentProviderSettingsDto settings) =>
        JsonSerializer.Serialize(settings, JsonOptions);

    public static PaymentProviderSettingsDto Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new PaymentProviderSettingsDto();

        return JsonSerializer.Deserialize<PaymentProviderSettingsDto>(json, JsonOptions) ?? new PaymentProviderSettingsDto();
    }

    public static PaymentProviderRuntimeSettings ToRuntime(PaymentProviderType type, bool isSandbox, PaymentProviderSettingsDto dto)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(dto.MerchantId)) values["merchantId"] = dto.MerchantId;
        if (!string.IsNullOrWhiteSpace(dto.TerminalId)) values["terminalId"] = dto.TerminalId;
        if (!string.IsNullOrWhiteSpace(dto.Username)) values["username"] = dto.Username;
        if (!string.IsNullOrWhiteSpace(dto.Password)) values["password"] = dto.Password;
        if (!string.IsNullOrWhiteSpace(dto.ClientId)) values["clientId"] = dto.ClientId;
        if (!string.IsNullOrWhiteSpace(dto.ClientSecret)) values["clientSecret"] = dto.ClientSecret;

        return new PaymentProviderRuntimeSettings(PaymentProviderKindMapper.ToKind(type), isSandbox, values);
    }

    /// <summary>Sandbox is never active in Production, even if the provider config flag is set.</summary>
    public static bool EffectiveSandbox(bool isSandbox, bool isProductionEnvironment) =>
        isSandbox && !isProductionEnvironment;

    public static long ToRials(decimal amount, string? currency = null)
    {
        var rounded = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        if (string.Equals(currency, "TOMAN", StringComparison.OrdinalIgnoreCase)
            || string.Equals(currency, "IRT", StringComparison.OrdinalIgnoreCase)
            || string.Equals(currency, "TMN", StringComparison.OrdinalIgnoreCase))
            rounded *= 10m;
        return (long)rounded;
    }
}

public static class PaymentUrlHelper
{
    public static string CallbackUrl(string baseUrl, PaymentProviderType provider) =>
        $"{baseUrl.TrimEnd('/')}/shop/payment/callback/{provider.ToString().ToLowerInvariant()}";

    public static string ReturnUrl(string baseUrl, PaymentProviderType provider, Guid orderId) =>
        $"{baseUrl.TrimEnd('/')}/shop/payment/return/{provider.ToString().ToLowerInvariant()}?orderId={orderId:D}";
}
