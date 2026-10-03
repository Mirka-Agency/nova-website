using CMS.Application.Payments;
using CMS.Infrastructure.Payments;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CMS.Web.Integration.Tests;

public sealed class ManualPaymentGatewayTests
{
    [Fact]
    public async Task ChargeAsync_Returns_Succeeded_With_TransactionId()
    {
        var gateway = new ManualPaymentGateway(NullLogger<ManualPaymentGateway>.Instance);

        var result = await gateway.ChargeAsync(new PaymentRequest(
            "ORD-TEST-1",
            150_000m,
            "IRR",
            "buyer@example.com",
            "test charge"));

        result.Succeeded.Should().BeTrue();
        result.TransactionId.Should().NotBeNullOrWhiteSpace();
        result.TransactionId!.Should().StartWith("MAN-");
        result.Provider.Should().Be(gateway.ProviderName);
        result.Message.Should().NotBeNullOrWhiteSpace();
    }
}
