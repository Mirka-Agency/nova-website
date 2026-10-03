using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CMS.Application.Payments;
using CMS.Infrastructure.Payments.Providers;

namespace CMS.Infrastructure.Payments.Providers;

public sealed class ZibalPaymentProvider : IPaymentProvider
{
    private const string BaseUrl = "https://gateway.zibal.ir";
    private readonly HttpClient _http;

    public ZibalPaymentProvider(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient(nameof(ZibalPaymentProvider));
    }

    public PaymentProviderKind Kind => PaymentProviderKind.Zibal;

    public async Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        PaymentProviderRuntimeSettings settings,
        CancellationToken cancellationToken = default)
    {
        var merchant = PaymentHttpHelper.RequireOrSandbox(
            settings.Values, "merchantId", settings.IsSandbox, PaymentSandboxDefaults.ZibalMerchantId);
        var body = new
        {
            merchant,
            amount = PaymentHttpHelper.ToRials(request.Amount, request.Currency),
            callbackUrl = request.CallbackUrl,
            orderId = request.AttemptId.ToString("N"),
            description = request.Description ?? request.OrderNumber
        };

        using var response = await _http.PostAsJsonAsync($"{BaseUrl}/v1/request", body, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ZibalResponse>(cancellationToken);

        if (result?.Result == 100 && result.TrackId.HasValue)
        {
            return new PaymentInitiationResult(
                true,
                $"{BaseUrl}/start/{result.TrackId}",
                result.TrackId.Value.ToString(),
                null,
                "زیبال");
        }

        return new PaymentInitiationResult(false, null, null, result?.Message ?? "خطا در اتصال به زیبال", "زیبال");
    }

    public async Task<PaymentVerificationResult> VerifyAsync(
        PaymentVerificationRequest request,
        PaymentProviderRuntimeSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (request.CallbackParameters.TryGetValue("success", out var success)
            && success is not "1" and not "true")
        {
            return new PaymentVerificationResult(false, null, "پرداخت توسط کاربر لغو شد.", "زیبال");
        }

        var trackId = request.ReferenceId;
        if (string.IsNullOrWhiteSpace(trackId))
        {
            request.CallbackParameters.TryGetValue("trackId", out trackId);
            request.CallbackParameters.TryGetValue("TrackId", out trackId);
        }

        if (string.IsNullOrWhiteSpace(trackId))
            return new PaymentVerificationResult(false, null, "trackId یافت نشد.", "زیبال");

        var merchant = PaymentHttpHelper.RequireOrSandbox(
            settings.Values, "merchantId", settings.IsSandbox, PaymentSandboxDefaults.ZibalMerchantId);
        var body = new { merchant, trackId = long.Parse(trackId) };

        using var response = await _http.PostAsJsonAsync($"{BaseUrl}/v1/verify", body, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ZibalResponse>(cancellationToken);

        if (result?.Result == 100)
        {
            return new PaymentVerificationResult(
                true,
                result.RefNumber?.ToString() ?? trackId,
                result.Message,
                "زیبال");
        }

        return new PaymentVerificationResult(false, null, result?.Message ?? "تأیید پرداخت زیبال ناموفق بود.", "زیبال");
    }

    private sealed class ZibalResponse
    {
        [JsonPropertyName("result")]
        public int Result { get; set; }

        [JsonPropertyName("trackId")]
        public long? TrackId { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("refNumber")]
        public long? RefNumber { get; set; }
    }
}
