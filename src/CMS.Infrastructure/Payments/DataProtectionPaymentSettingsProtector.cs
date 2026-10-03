using CMS.Application.Payments;
using Microsoft.AspNetCore.DataProtection;

namespace CMS.Infrastructure.Payments;

public sealed class DataProtectionPaymentSettingsProtector : IPaymentSettingsProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionPaymentSettingsProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("CMS.Shop.PaymentProviderSettings.v1");
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}
