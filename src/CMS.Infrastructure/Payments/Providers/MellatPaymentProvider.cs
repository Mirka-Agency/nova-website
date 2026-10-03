using System.Text;
using System.Xml.Linq;
using CMS.Application.Payments;
using CMS.Infrastructure.Payments.Providers;

namespace CMS.Infrastructure.Payments.Providers;

public sealed class MellatPaymentProvider : IPaymentProvider
{
    private const string SoapEndpoint = "https://bpm.shaparak.ir/pgwchannel/services/pgw";
    private readonly HttpClient _http;

    public MellatPaymentProvider(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient(nameof(MellatPaymentProvider));
    }

    public PaymentProviderKind Kind => PaymentProviderKind.Mellat;

    public async Task<PaymentInitiationResult> InitiateAsync(
        PaymentInitiationRequest request,
        PaymentProviderRuntimeSettings settings,
        CancellationToken cancellationToken = default)
    {
        var terminalId = long.Parse(PaymentHttpHelper.Require(settings.Values, "terminalId"));
        var username = PaymentHttpHelper.Require(settings.Values, "username");
        var password = PaymentHttpHelper.Require(settings.Values, "password");

        var localDate = DateTime.Now.ToString("yyyyMMdd");
        var localTime = DateTime.Now.ToString("HHmmss");
        var orderId = request.AttemptId.GetHashCode() & int.MaxValue;
        if (orderId == 0) orderId = 1;

        var envelope = BuildEnvelope("bpPayRequest", $@"
            <terminalId>{terminalId}</terminalId>
            <userName>{Escape(username)}</userName>
            <userPassword>{Escape(password)}</userPassword>
            <orderId>{orderId}</orderId>
            <amount>{PaymentHttpHelper.ToRials(request.Amount, request.Currency)}</amount>
            <localDate>{localDate}</localDate>
            <localTime>{localTime}</localTime>
            <additionalData>{Escape(request.OrderNumber)}</additionalData>
            <callBackUrl>{Escape(request.CallbackUrl)}</callBackUrl>
            <payerId>0</payerId>");

        var responseXml = await PostSoapAsync(envelope, cancellationToken);
        var (code, refId) = ParsePayResponse(responseXml);

        if (code == "0" && !string.IsNullOrWhiteSpace(refId))
        {
            return new PaymentInitiationResult(
                true,
                $"https://bpm.shaparak.ir/pgwchannel/startpay.mellat?RefId={Uri.EscapeDataString(refId)}",
                refId,
                null,
                "به‌پرداخت ملت");
        }

        return new PaymentInitiationResult(false, null, null, $"خطای به‌پرداخت ملت: {code}", "به‌پرداخت ملت");
    }

    public async Task<PaymentVerificationResult> VerifyAsync(
        PaymentVerificationRequest request,
        PaymentProviderRuntimeSettings settings,
        CancellationToken cancellationToken = default)
    {
        var refId = GetParam(request.CallbackParameters, "RefId", "refId") ?? request.ReferenceId;
        var resCode = GetParam(request.CallbackParameters, "ResCode", "resCode");
        var saleOrderId = GetParam(request.CallbackParameters, "SaleOrderId", "saleOrderId");
        var saleReferenceId = GetParam(request.CallbackParameters, "SaleReferenceId", "saleReferenceId");

        if (resCode is not null and not "0")
            return new PaymentVerificationResult(false, null, "پرداخت توسط کاربر لغو یا ناموفق بود.", "به‌پرداخت ملت");

        if (string.IsNullOrWhiteSpace(saleOrderId) || string.IsNullOrWhiteSpace(saleReferenceId))
            return new PaymentVerificationResult(false, null, "پارامترهای بازگشت از درگاه ناقص است.", "به‌پرداخت ملت");

        var terminalId = long.Parse(PaymentHttpHelper.Require(settings.Values, "terminalId"));
        var username = PaymentHttpHelper.Require(settings.Values, "username");
        var password = PaymentHttpHelper.Require(settings.Values, "password");

        var verifyEnvelope = BuildEnvelope("bpVerifyRequest", $@"
            <terminalId>{terminalId}</terminalId>
            <userName>{Escape(username)}</userName>
            <userPassword>{Escape(password)}</userPassword>
            <orderId>{saleOrderId}</orderId>
            <saleOrderId>{saleOrderId}</saleOrderId>
            <saleReferenceId>{saleReferenceId}</saleReferenceId>");

        var verifyXml = await PostSoapAsync(verifyEnvelope, cancellationToken);
        var verifyCode = ParseSingleReturn(verifyXml);

        if (verifyCode != "0")
            return new PaymentVerificationResult(false, null, $"تأیید به‌پرداخت ملت ناموفق: {verifyCode}", "به‌پرداخت ملت");

        var settleEnvelope = BuildEnvelope("bpSettleRequest", $@"
            <terminalId>{terminalId}</terminalId>
            <userName>{Escape(username)}</userName>
            <userPassword>{Escape(password)}</userPassword>
            <orderId>{saleOrderId}</orderId>
            <saleOrderId>{saleOrderId}</saleOrderId>
            <saleReferenceId>{saleReferenceId}</saleReferenceId>");

        var settleXml = await PostSoapAsync(settleEnvelope, cancellationToken);
        var settleCode = ParseSingleReturn(settleXml);

        if (settleCode is "0" or "45")
        {
            return new PaymentVerificationResult(
                true,
                saleReferenceId ?? refId,
                "پرداخت تأیید و تسویه شد.",
                "به‌پرداخت ملت");
        }

        return new PaymentVerificationResult(false, null, $"تسویه به‌پرداخت ملت ناموفق: {settleCode}", "به‌پرداخت ملت");
    }

    private async Task<string> PostSoapAsync(string envelope, CancellationToken cancellationToken)
    {
        using var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", "");
        using var response = await _http.PostAsync(SoapEndpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static string BuildEnvelope(string action, string bodyInner) =>
        $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:int=""http://interfaces.core.sw.bps.com/"">
  <soapenv:Header/>
  <soapenv:Body>
    <int:{action}>
      {bodyInner}
    </int:{action}>
  </soapenv:Body>
</soapenv:Envelope>";

    private static string Escape(string value) =>
        System.Security.SecurityElement.Escape(value) ?? value;

    private static (string Code, string? RefId) ParsePayResponse(string xml)
    {
        var doc = XDocument.Parse(xml);
        var returnValue = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "return")?.Value ?? string.Empty;
        var parts = returnValue.Split(',', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : (returnValue, null);
    }

    private static string ParseSingleReturn(string xml)
    {
        var doc = XDocument.Parse(xml);
        return doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "return")?.Value ?? string.Empty;
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
}
