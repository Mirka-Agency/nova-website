using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMS.Infrastructure.Payments.Providers;

internal static class PaymentHttpHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null
    };

    public static async Task<(TResponse? Body, string? ErrorText, int StatusCode)> PostJsonAsync<TRequest, TResponse>(
        HttpClient http,
        string url,
        TRequest body,
        CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(url, body, JsonOptions, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        TResponse? parsed = default;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                parsed = JsonSerializer.Deserialize<TResponse>(raw, JsonOptions);
            }
            catch (JsonException)
            {
                // Keep raw body for diagnostics when the gateway returns an unexpected shape.
            }
        }

        if (response.IsSuccessStatusCode)
            return (parsed, null, (int)response.StatusCode);

        return (parsed, string.IsNullOrWhiteSpace(raw) ? response.ReasonPhrase : raw, (int)response.StatusCode);
    }

    public static long ToRials(decimal amount, string? currency = null)
    {
        var rounded = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        if (IsToman(currency))
            rounded *= 10m;
        return (long)rounded;
    }

    public static bool IsToman(string? currency) =>
        string.Equals(currency, "TOMAN", StringComparison.OrdinalIgnoreCase)
        || string.Equals(currency, "IRT", StringComparison.OrdinalIgnoreCase)
        || string.Equals(currency, "TMN", StringComparison.OrdinalIgnoreCase);

    public static string Require(IReadOnlyDictionary<string, string> settings, string key)
    {
        if (!settings.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Payment setting '{key}' is required.");
        return value.Trim();
    }

    /// <summary>
    /// Returns the configured setting, or a known sandbox default when sandbox mode is on and the value is empty.
    /// </summary>
    public static string RequireOrSandbox(
        IReadOnlyDictionary<string, string> settings,
        string key,
        bool isSandbox,
        string? sandboxDefault)
    {
        if (settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            return value.Trim();

        if (isSandbox && !string.IsNullOrWhiteSpace(sandboxDefault))
            return sandboxDefault;

        throw new InvalidOperationException($"Payment setting '{key}' is required.");
    }
}

/// <summary>
/// Official / documented sandbox credentials. SEP, Mellat, and SnapPay have no public sandbox merchant IDs.
/// </summary>
internal static class PaymentSandboxDefaults
{
    /// <summary>Zarinpal sandbox accepts any 36-char UUID.</summary>
    public const string ZarinpalMerchantId = "00000000-0000-0000-0000-000000000000";

    /// <summary>Zibal sandbox merchant code.</summary>
    public const string ZibalMerchantId = "zibal";
}

internal sealed class ZarinpalRequestBody
{
    [JsonPropertyName("merchant_id")]
    public string MerchantId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("callback_url")]
    public string CallbackUrl { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Currency { get; set; }

    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Metadata { get; set; }
}

internal sealed class ZarinpalResponse
{
    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    [JsonPropertyName("errors")]
    public JsonElement Errors { get; set; }

    public int? DataCode =>
        Data.ValueKind == JsonValueKind.Object && Data.TryGetProperty("code", out var code) && code.TryGetInt32(out var value)
            ? value
            : null;

    public string? DataAuthority =>
        Data.ValueKind == JsonValueKind.Object && Data.TryGetProperty("authority", out var authority)
            ? authority.GetString()
            : null;

    public string? DataMessage =>
        Data.ValueKind == JsonValueKind.Object && Data.TryGetProperty("message", out var message)
            ? message.GetString()
            : null;

    public long? DataRefId =>
        Data.ValueKind == JsonValueKind.Object && Data.TryGetProperty("ref_id", out var refId) && refId.TryGetInt64(out var value)
            ? value
            : null;

    public string? ErrorMessage
    {
        get
        {
            if (Errors.ValueKind == JsonValueKind.Object)
            {
                if (Errors.TryGetProperty("message", out var message))
                {
                    var text = message.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }

                if (Errors.TryGetProperty("validations", out var validations) && validations.ValueKind == JsonValueKind.Array)
                {
                    var parts = validations.EnumerateArray()
                        .Select(v => v.TryGetProperty("message", out var m) ? m.GetString() : null)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToArray();
                    if (parts.Length > 0)
                        return string.Join("؛ ", parts);
                }
            }

            if (Errors.ValueKind == JsonValueKind.Array)
            {
                var parts = Errors.EnumerateArray()
                    .Select(e => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("message", out var m)
                        ? m.GetString()
                        : e.GetString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();
                if (parts.Length > 0)
                    return string.Join("؛ ", parts);
            }

            return DataMessage;
        }
    }
}
