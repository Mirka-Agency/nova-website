using CMS.Application.Payments;

namespace CMS.Infrastructure.Payments;

public sealed class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly IReadOnlyDictionary<PaymentProviderKind, IPaymentProvider> _providers;

    public PaymentProviderFactory(IEnumerable<IPaymentProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.Kind);
    }

    public IPaymentProvider Get(PaymentProviderKind kind) =>
        _providers.TryGetValue(kind, out var provider)
            ? provider
            : throw new InvalidOperationException($"Payment provider '{kind}' is not registered.");

    public IReadOnlyCollection<IPaymentProvider> GetAll() => _providers.Values.ToList();
}
