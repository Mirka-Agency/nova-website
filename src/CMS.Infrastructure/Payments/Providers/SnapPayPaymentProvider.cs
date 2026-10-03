using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CMS.Application.Payments;
using CMS.Infrastructure.Payments.Providers;

namespace CMS.Infrastructure.Payments.Providers;

public sealed class SnapPayPaymentProvider : IPaymentProvider
{
    private const string BaseUrl = "https://api.snapppay.ir/api/online/v1/payment";
    private readonly HttpClient _http;

    public SnapPayPaymentProvider(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient(nameof(SnapPayPaymentProvider));
    }

    public PaymentProviderKind Kind => PaymentProviderKind.SnapPay;

    public async Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        PaymentProviderRuntimeSettings settings,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(settings, cancellationToken);
        var body = new
        {
            amount = PaymentHttpHelper.ToRials(request.Amount, request.Currency),
            mobile = request.CustomerPhone,
            returnURL = request.ReturnUrl ?? request.CallbackUrl,
            transactionId = request.AttemptId.ToString("N"),
            description = request.Description ?? request.OrderNumber
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/token");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = JsonContent.Create(body);

        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SnapPayTokenResponse>(cancellationToken);

        if (!string.IsNullOrWhiteSpace(result?.Response?.PaymentToken))
        {
            return new PaymentInitiationResult(
                true,
                result.Response!.PaymentPageUrl ?? $"https://fms-gateway-staging.apps.public.okd4.teh-1.snappcloud.io/ipg?token={result.Response.PaymentToken}",
                result.Response.PaymentToken,
                null,
                "اسنپ‌پی");
        }

        return new PaymentInitiationResult(false, null, null, result?.ErrorData?.Message ?? "خطا در اتصال به اسنپ‌پی", "اسنپ‌پی");
    }

    public async Task<PaymentVerificationResult> VerifyAsync(
        PaymentVerificationRequest request,
        PaymentProviderRuntimeSettings settings,
        CancellationToken cancellationToken = default)
    {
        var paymentToken = request.ReferenceId;
        if (string.IsNullOrWhiteSpace(paymentToken))
        {
            request.CallbackParameters.TryGetValue("paymentToken", out paymentToken);
            request.CallbackParameters.TryGetValue("token", out paymentToken);
        }

        if (string.IsNullOrWhiteSpace(paymentToken))
            return new PaymentVerificationResult(false, null, "توکن اسنپ‌پی یافت نشد.", "اسنپ‌پی");

        var accessToken = await GetAccessTokenAsync(settings, cancellationToken);
        var body = new { paymentToken };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/verify");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpRequest.Content = JsonContent.Create(body);

        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new PaymentVerificationResult(false, null, "تأیید پرداخت اسنپ‌پی ناموفق بود.", "اسنپ‌پی");

        var result = await response.Content.ReadFromJsonAsync<SnapPayVerifyResponse>(cancellationToken);
        if (result?.Successful == true)
        {
            return new PaymentVerificationResult(
                true,
                result.Response?.TransactionId ?? paymentToken,
                "پرداخت تأیید شد.",
                "اسنپ‌پی");
        }

        return new PaymentVerificationResult(false, null, result?.ErrorData?.Message ?? "تأیید پرداخت اسنپ‌پی ناموفق بود.", "اسنپ‌پی");
    }

    private async Task<string> GetAccessTokenAsync(PaymentProviderRuntimeSettings settings, CancellationToken cancellationToken)
    {
        var clientId = PaymentHttpHelper.Require(settings.Values, "clientId");
        var clientSecret = PaymentHttpHelper.Require(settings.Values, "clientSecret");
        var username = PaymentHttpHelper.Require(settings.Values, "username");
        var password = PaymentHttpHelper.Require(settings.Values, "password");

        var body = new
        {
            grant_type = "password",
            scope = "online-merchant",
            username,
            password,
            client_id = clientId,
            client_secret = clientSecret
        };

        using var response = await _http.PostAsJsonAsync("https://api.snapppay.ir/api/online/v1/oauth/token", body, cancellationToken);
        response.EnsureSuccessStatusCode();
        var tokenResponse = await response.Content.ReadFromJsonAsync<SnapPayOAuthResponse>(cancellationToken);
        if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
            throw new InvalidOperationException("SnapPay OAuth token was not returned.");

        return tokenResponse.AccessToken;
    }

    private sealed class SnapPayOAuthResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    private sealed class SnapPayTokenResponse
    {
        [JsonPropertyName("successful")]
        public bool Successful { get; set; }

        [JsonPropertyName("response")]
        public SnapPayTokenData? Response { get; set; }

        [JsonPropertyName("errorData")]
        public SnapPayError? ErrorData { get; set; }
    }

    private sealed class SnapPayTokenData
    {
        [JsonPropertyName("paymentToken")]
        public string? PaymentToken { get; set; }

        [JsonPropertyName("paymentPageUrl")]
        public string? PaymentPageUrl { get; set; }
    }

    private sealed class SnapPayVerifyResponse
    {
        [JsonPropertyName("successful")]
        public bool Successful { get; set; }

        [JsonPropertyName("response")]
        public SnapPayVerifyData? Response { get; set; }

        [JsonPropertyName("errorData")]
        public SnapPayError? ErrorData { get; set; }
    }

    private sealed class SnapPayVerifyData
    {
        [JsonPropertyName("transactionId")]
        public string? TransactionId { get; set; }
    }

    private sealed class SnapPayError
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
