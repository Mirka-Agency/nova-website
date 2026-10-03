namespace CMS.Application.Sms;

public sealed record SmsMessage(
    string Mobile,
    string Text,
    IReadOnlyList<string>? AdditionalMobiles = null);

public sealed record SmsSendResult(
    bool Succeeded,
    string Provider,
    string? MessageId = null,
    string? ProviderMessage = null,
    bool Skipped = false)
{
    public static SmsSendResult Skip(string provider, string reason) =>
        new(false, provider, ProviderMessage: reason, Skipped: true);

    public static SmsSendResult Ok(string provider, string? messageId, string? providerMessage = null) =>
        new(true, provider, messageId, providerMessage);

    public static SmsSendResult Fail(string provider, string reason) =>
        new(false, provider, ProviderMessage: reason);
}

public interface ISmsSender
{
    Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
}

public interface ISmsProvider
{
    string ProviderId { get; }
    bool IsConfigured { get; }
    Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
}

public static class SmsProviders
{
    public const string None = "";
    public const string SmsIr = "SmsIr";
}

public sealed class SmsOptions
{
    public const string SectionName = "Sms";

    /// <summary>Active provider id, e.g. <see cref="SmsProviders.SmsIr"/>. Empty disables sending.</summary>
    public string Provider { get; set; } = SmsProviders.None;

    /// <summary>CSV of admin mobile numbers for operational SMS alerts.</summary>
    public string AdminNotifyPhones { get; set; } = string.Empty;

    public IReadOnlyList<string> GetAdminNotifyPhoneList()
    {
        if (string.IsNullOrWhiteSpace(AdminNotifyPhones))
            return [];

        return AdminNotifyPhones
            .Split([',', ';', ' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SmsPhoneNormalizer.Normalize)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToList();
    }
}

public sealed class SmsIrOptions
{
    public const string SectionName = "Sms:SmsIr";

    /// <summary>API key from sms.ir developer panel (IPE.SmsIr client).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Sender line number from the sms.ir panel.</summary>
    public long LineNumber { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey)
        && LineNumber > 0;
}

public static class SmsPhoneNormalizer
{
    /// <summary>Normalizes Iranian mobiles to 09xxxxxxxxx when possible; otherwise returns trimmed digits.</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return null;

        if (digits.StartsWith("98", StringComparison.Ordinal) && digits.Length >= 12)
            digits = "0" + digits[2..];
        else if (digits.StartsWith("9", StringComparison.Ordinal) && digits.Length == 10)
            digits = "0" + digits;

        return digits;
    }
}
