using CMS.Application.Payments;

namespace CMS.Infrastructure.Payments.Providers;

public sealed class ZarinpalPaymentProvider : IPaymentProvider
{
    private const long MinimumAmountRials = 10_000;
    private readonly HttpClient _http;

    public ZarinpalPaymentProvider(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient(nameof(ZarinpalPaymentProvider));
    }

    public PaymentProviderKind Kind => PaymentProviderKind.Zarinpal;

    public async Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        PaymentProviderRuntimeSettings settings,
        CancellationToken cancellationToken = default)
    {
        var merchantId = PaymentHttpHelper.RequireOrSandbox(
            settings.Values, "merchantId", settings.IsSandbox, PaymentSandboxDefaults.ZarinpalMerchantId);
        var amount = PaymentHttpHelper.ToRials(request.Amount, request.Currency);
        if (amount < MinimumAmountRials)
        {
            return new PaymentInitiationResult(
                false,
                null,
                null,
                $"حداقل مبلغ پرداخت زرین‌پال {MinimumAmountRials:N0} ریال است.",
                "زرین‌پال");
        }

        var baseUrl = settings.IsSandbox ? "https://sandbox.zarinpal.com" : "https://api.zarinpal.com";
        var body = new ZarinpalRequestBody
        {
            MerchantId = merchantId,
            Amount = amount,
            CallbackUrl = request.CallbackUrl,
            Description = string.IsNullOrWhiteSpace(request.Description) ? request.OrderNumber : request.Description,
            Currency = "IRR"
        };

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(request.CustomerEmail)
            && !request.CustomerEmail.EndsWith("@customers.local", StringComparison.OrdinalIgnoreCase))
            metadata["email"] = request.CustomerEmail!;
        if (!string.IsNullOrWhiteSpace(request.CustomerPhone))
            metadata["mobile"] = NormalizeMobile(request.CustomerPhone!);
        if (metadata.Count > 0)
            body.Metadata = metadata;

        var (response, errorText, statusCode) = await PaymentHttpHelper.PostJsonAsync<ZarinpalRequestBody, ZarinpalResponse>(
            _http,
            $"{baseUrl}/pg/v4/payment/request.json",
            body,
            cancellationToken);

        if (response?.DataCode == 100 && !string.IsNullOrWhiteSpace(response.DataAuthority))
        {
            var startBase = settings.IsSandbox ? "https://sandbox.zarinpal.com" : "https://www.zarinpal.com";
            return new PaymentInitiationResult(
                true,
                $"{startBase}/pg/StartPay/{response.DataAuthority}",
                response.DataAuthority,
                null,
                "زرین‌پال");
        }

        var message = response?.ErrorMessage
            ?? SummarizeGatewayError(statusCode, errorText)
            ?? "خطا در اتصال به زرین‌پال";

        return new PaymentInitiationResult(false, null, null, message, "زرین‌پال");
    }

    public async Task<PaymentVerificationResult> VerifyAsync(
        PaymentVerificationRequest request,
        PaymentProviderRuntimeSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (request.CallbackParameters.TryGetValue("Status", out var status)
            && !string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentVerificationResult(false, null, "پرداخت توسط کاربر لغو شد.", "زرین‌پال");
        }

        var authority = request.ReferenceId;
        if (string.IsNullOrWhiteSpace(authority))
        {
            request.CallbackParameters.TryGetValue("Authority", out authority);
            request.CallbackParameters.TryGetValue("authority", out authority);
        }

        if (string.IsNullOrWhiteSpace(authority))
            return new PaymentVerificationResult(false, null, "Authority یافت نشد.", "زرین‌پال");

        var merchantId = PaymentHttpHelper.RequireOrSandbox(
            settings.Values, "merchantId", settings.IsSandbox, PaymentSandboxDefaults.ZarinpalMerchantId);
        var baseUrl = settings.IsSandbox ? "https://sandbox.zarinpal.com" : "https://api.zarinpal.com";
        var body = new
        {
            merchant_id = merchantId,
            amount = PaymentHttpHelper.ToRials(request.Amount, request.Currency),
            authority
        };

        var (result, errorText, statusCode) = await PaymentHttpHelper.PostJsonAsync<object, ZarinpalResponse>(
            _http,
            $"{baseUrl}/pg/v4/payment/verify.json",
            body,
            cancellationToken);

        if (result?.DataCode is 100 or 101)
        {
            return new PaymentVerificationResult(
                true,
                result.DataRefId?.ToString() ?? authority,
                result.DataMessage,
                "زرین‌پال");
        }

        var message = result?.ErrorMessage
            ?? SummarizeGatewayError(statusCode, errorText)
            ?? "تأیید پرداخت زرین‌پال ناموفق بود.";

        return new PaymentVerificationResult(false, null, message, "زرین‌پال");
    }

    private static string NormalizeMobile(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("98") && digits.Length == 12)
            return "0" + digits[2..];
        return digits;
    }

    private static string? SummarizeGatewayError(int statusCode, string? errorText)
    {
        if (string.IsNullOrWhiteSpace(errorText))
            return statusCode is >= 400 ? $"پاسخ نامعتبر زرین‌پال ({statusCode})" : null;

        // Prefer a short message for UI; full JSON stays useful for logs via exception middleware elsewhere.
        if (errorText.Length <= 280)
            return errorText;

        return $"پاسخ نامعتبر زرین‌پال ({statusCode})";
    }
}
