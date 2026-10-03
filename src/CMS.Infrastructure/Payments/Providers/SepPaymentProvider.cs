using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CMS.Application.Payments;
using CMS.Infrastructure.Payments.Providers;

namespace CMS.Infrastructure.Payments.Providers;

public sealed class SepPaymentProvider : IPaymentProvider
{
    private const string TokenUrl = "https://sep.shaparak.ir/onlinepg/onlinepg";
    private readonly HttpClient _http;

    public SepPaymentProvider(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient(nameof(SepPaymentProvider));
    }

    public PaymentProviderKind Kind => PaymentProviderKind.Sep;

    public async Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        PaymentProviderRuntimeSettings settings,
        CancellationToken cancellationToken = default)
    {
        var terminalId = PaymentHttpHelper.Require(settings.Values, "terminalId");
        var body = new
        {
            action = "token",
            TerminalId = terminalId,
            Amount = PaymentHttpHelper.ToRials(request.Amount, request.Currency),
            ResNum = request.AttemptId.ToString("N"),
            RedirectUrl = request.CallbackUrl,
            CellNumber = request.CustomerPhone
        };

        using var response = await _http.PostAsJsonAsync(TokenUrl, body, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SepTokenResponse>(cancellationToken);

        if (!string.IsNullOrWhiteSpace(result?.Token))
        {
            return new PaymentInitiationResult(
                true,
                $"https://sep.shaparak.ir/OnlinePG/SendToken?token={Uri.EscapeDataString(result.Token)}",
                result.Token,
                null,
                "سپ");
        }

        return new PaymentInitiationResult(false, null, null, result?.ErrorDesc ?? "خطا در دریافت توکن سپ", "سپ");
    }

    public async Task<PaymentVerificationResult> VerifyAsync(
        PaymentVerificationRequest request,
        PaymentProviderRuntimeSettings settings,
        CancellationToken cancellationToken = default)
    {
        var refNum = GetParam(request.CallbackParameters, "RefNum", "refNum");
        if (string.IsNullOrWhiteSpace(refNum))
            return new PaymentVerificationResult(false, null, "RefNum یافت نشد.", "سپ");

        var terminalId = PaymentHttpHelper.Require(settings.Values, "terminalId");
        var body = new
        {
            RefNum = refNum,
            TerminalNumber = terminalId
        };

        using var response = await _http.PostAsJsonAsync("https://sep.shaparak.ir/verifyTxnRandomSessionkey/ipg/VerifyTransaction", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new PaymentVerificationResult(false, null, "تأیید تراکنش سپ ناموفق بود.", "سپ");

        var result = await response.Content.ReadFromJsonAsync<SepVerifyResponse>(cancellationToken);
        if (result?.Success == true && result.TransactionDetail?.OrginalAmount == PaymentHttpHelper.ToRials(request.Amount, request.Currency))
        {
            return new PaymentVerificationResult(true, refNum, "پرداخت تأیید شد.", "سپ");
        }

        return new PaymentVerificationResult(false, null, result?.ResultDescription ?? "تأیید پرداخت سپ ناموفق بود.", "سپ");
    }

    private static string? GetParam(IReadOnlyDictionary<string, string> parameters, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private sealed class SepTokenResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("errorDesc")]
        public string? ErrorDesc { get; set; }
    }

    private sealed class SepVerifyResponse
    {
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        [JsonPropertyName("ResultDescription")]
        public string? ResultDescription { get; set; }

        [JsonPropertyName("TransactionDetail")]
        public SepTransactionDetail? TransactionDetail { get; set; }
    }

    private sealed class SepTransactionDetail
    {
        [JsonPropertyName("OrginalAmount")]
        public long OrginalAmount { get; set; }
    }
}
