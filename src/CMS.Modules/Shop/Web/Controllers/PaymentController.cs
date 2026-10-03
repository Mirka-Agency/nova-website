using CMS.Application.Common.Features;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Payments;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Shop.Web.Controllers;

[Route("shop/payment")]
public class PaymentController : Controller
{
    private readonly IPaymentOrchestrator _payments;
    private readonly IFeatureManager _features;

    public PaymentController(IPaymentOrchestrator payments, IFeatureManager features)
    {
        _payments = payments;
        _features = features;
    }

    [HttpGet("callback/{provider}")]
    [HttpPost("callback/{provider}")]
    public async Task<IActionResult> Callback(string provider, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var providerType = ParseProvider(provider);
        if (providerType is null)
            return BadRequest();

        var parameters = CollectParameters();
        var baseUrl = BuildBaseUrl();
        var result = await _payments.VerifyCallbackAsync(
            new PaymentCallbackContext(providerType.Value, null, parameters),
            baseUrl,
            cancellationToken);

        return RedirectToAction(nameof(Return), new
        {
            provider = provider.ToLowerInvariant(),
            orderId = result.OrderId,
            success = result.Succeeded
        });
    }

    [HttpGet("return/{provider}")]
    public async Task<IActionResult> Return(
        string provider,
        Guid orderId,
        bool? success,
        CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var providerType = ParseProvider(provider);
        if (providerType is null)
            return BadRequest();

        var parameters = CollectParameters();
        var baseUrl = BuildBaseUrl();
        var result = await _payments.CompleteReturnAsync(orderId, providerType.Value, parameters, baseUrl, cancellationToken);

        ViewData["Title"] = "نتیجه پرداخت";
        return View(new CheckoutResultViewModel
        {
            OrderId = result.OrderId,
            OrderNumber = result.OrderNumber,
            PaymentSucceeded = result.Succeeded,
            PaymentMessage = result.Message,
            TransactionId = result.TransactionId
        });
    }

    private static PaymentProviderType? ParseProvider(string provider) =>
        Enum.TryParse<PaymentProviderType>(provider, ignoreCase: true, out var type) ? type : null;

    private Dictionary<string, string> CollectParameters()
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in Request.Query)
        {
            if (!string.IsNullOrWhiteSpace(value))
                parameters[key] = value.ToString();
        }

        if (Request.HasFormContentType)
        {
            foreach (var key in Request.Form.Keys)
            {
                var value = Request.Form[key].ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    parameters[key] = value;
            }
        }

        return parameters;
    }

    private string BuildBaseUrl()
    {
        var request = HttpContext.Request;
        return $"{request.Scheme}://{request.Host}{request.PathBase}";
    }
}
