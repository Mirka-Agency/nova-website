using CMS.Application.Payments;
using Microsoft.Extensions.Logging;

namespace CMS.Infrastructure.Payments;

/// <summary>
/// Manual / offline payment provider for Catalog ecommerce bootstrap.
/// Real gateways (Zarinpal, etc.) plug in via <see cref="IPaymentGateway"/>.
/// </summary>
public sealed class ManualPaymentGateway : IPaymentGateway
{
    private readonly ILogger<ManualPaymentGateway> _logger;

    public ManualPaymentGateway(ILogger<ManualPaymentGateway> logger)
    {
        _logger = logger;
    }

    public string ProviderName => "پرداخت دستی";

    public Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var tx = $"MAN-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        _logger.LogInformation(
            "Manual payment recorded for order {OrderNumber} amount {Amount} {Currency} tx {Tx}",
            request.OrderNumber,
            request.Amount,
            request.Currency,
            tx);

        return Task.FromResult(new PaymentResult(true, tx, "پرداخت دستی ثبت شد.", ProviderName));
    }
}
