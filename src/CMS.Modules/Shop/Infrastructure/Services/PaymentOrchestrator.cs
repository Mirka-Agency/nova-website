using CMS.Application.Payments;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Payments;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Infrastructure.Payments;
using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class PaymentOrchestrator : IPaymentOrchestrator
{
    private readonly ShopDbContext _db;
    private readonly IPaymentConfigService _config;
    private readonly IPaymentProviderFactory _providers;
    private readonly ICartService _cart;
    private readonly IShopOrderNotifier _notifier;
    private readonly IHostEnvironment _environment;

    public PaymentOrchestrator(
        ShopDbContext db,
        IPaymentConfigService config,
        IPaymentProviderFactory providers,
        ICartService cart,
        IShopOrderNotifier notifier,
        IHostEnvironment environment)
    {
        _db = db;
        _config = config;
        _providers = providers;
        _cart = cart;
        _notifier = notifier;
        _environment = environment;
    }

    public async Task<PaymentInitiationResponse> InitiateForOrderAsync(
        Guid orderId,
        Guid paymentProviderConfigId,
        string callbackBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), orderId);

        if (!order.IsUnpaidPending)
            throw new DomainException("سفارش قابل پرداخت نیست.");

        if (order.IsPaymentExpired(DateTime.UtcNow))
            throw new DomainException("مهلت پرداخت این سفارش به پایان رسیده است.");

        var providerConfig = await _config.GetAsync(paymentProviderConfigId, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentProviderConfig), paymentProviderConfigId);

        if (!providerConfig.IsEnabled)
            throw new DomainException("درگاه پرداخت انتخاب‌شده فعال نیست.");

        var attempt = PaymentAttempt.Create(
            order.Id,
            providerConfig.Id,
            providerConfig.ProviderType,
            order.TotalAmount,
            order.Currency);

        _db.PaymentAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);

        var runtime = CreateRuntime(providerConfig);
        var provider = _providers.Get(runtime.Kind);
        var callbackUrl = PaymentUrlHelper.CallbackUrl(callbackBaseUrl, providerConfig.ProviderType);
        var returnUrl = PaymentUrlHelper.ReturnUrl(callbackBaseUrl, providerConfig.ProviderType, order.Id);

        var initiation = await provider.InitiateAsync(
            new PaymentInitiationRequest(
                attempt.Id,
                order.OrderNumber,
                order.TotalAmount,
                order.Currency,
                order.CustomerEmail,
                order.CustomerPhone,
                $"Order {order.OrderNumber}",
                callbackUrl,
                returnUrl),
            runtime,
            cancellationToken);

        if (!initiation.Succeeded || string.IsNullOrWhiteSpace(initiation.RedirectUrl))
        {
            attempt.MarkFailed(initiation.Message ?? "خطا در اتصال به درگاه");
            await _db.SaveChangesAsync(cancellationToken);
            return new PaymentInitiationResponse(order.Id, order.OrderNumber, null, initiation.Message);
        }

        attempt.MarkRedirected(initiation.ReferenceId ?? attempt.Id.ToString("N"));
        await _db.SaveChangesAsync(cancellationToken);

        return new PaymentInitiationResponse(order.Id, order.OrderNumber, initiation.RedirectUrl, null);
    }

    public async Task<PaymentVerificationResponse> VerifyCallbackAsync(
        PaymentCallbackContext context,
        string callbackBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderAsync(context, cancellationToken);
        if (order is null)
            return new PaymentVerificationResponse(Guid.Empty, string.Empty, false, "سفارش یافت نشد.", null);

        return await VerifyOrderAsync(order, context.ProviderType, context.Parameters, cancellationToken);
    }

    public async Task<PaymentVerificationResponse> CompleteReturnAsync(
        Guid orderId,
        PaymentProviderType providerType,
        IReadOnlyDictionary<string, string> queryParameters,
        string callbackBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
            return new PaymentVerificationResponse(orderId, string.Empty, false, "سفارش یافت نشد.", null);

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            return new PaymentVerificationResponse(
                order.Id,
                order.OrderNumber,
                true,
                "پرداخت قبلاً تأیید شده است.",
                order.PaymentTransactionId);
        }

        return await VerifyOrderAsync(order, providerType, queryParameters, cancellationToken);
    }

    private async Task<PaymentVerificationResponse> VerifyOrderAsync(
        Order order,
        PaymentProviderType providerType,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            return new PaymentVerificationResponse(
                order.Id,
                order.OrderNumber,
                true,
                "پرداخت قبلاً تأیید شده است.",
                order.PaymentTransactionId);
        }

        if (!order.IsUnpaidPending || order.IsPaymentExpired(DateTime.UtcNow))
            return new PaymentVerificationResponse(order.Id, order.OrderNumber, false, "مهلت پرداخت به پایان رسیده است.", null);

        var providerConfig = await _config.GetByTypeAsync(providerType, cancellationToken)
            ?? throw new DomainException("درگاه پرداخت پیکربندی نشده است.");

        var attempt = await _db.PaymentAttempts
            .Where(a => a.OrderId == order.Id && a.ProviderType == providerType)
            .OrderByDescending(a => a.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (attempt is null)
            return new PaymentVerificationResponse(order.Id, order.OrderNumber, false, "تراکنش پرداخت یافت نشد.", null);

        if (attempt.Status == PaymentAttemptStatus.Verified)
        {
            return new PaymentVerificationResponse(
                order.Id,
                order.OrderNumber,
                true,
                "پرداخت تأیید شده است.",
                attempt.TransactionId);
        }

        var runtime = CreateRuntime(providerConfig);
        var provider = _providers.Get(runtime.Kind);
        var payload = JsonSerializer.Serialize(parameters);

        var verification = await provider.VerifyAsync(
            new PaymentVerificationRequest(
                attempt.Id,
                order.OrderNumber,
                order.TotalAmount,
                order.Currency,
                attempt.ReferenceId,
                parameters),
            runtime,
            cancellationToken);

        if (!verification.Succeeded)
        {
            attempt.MarkFailed(verification.Message, payload);
            await _db.SaveChangesAsync(cancellationToken);
            return new PaymentVerificationResponse(order.Id, order.OrderNumber, false, verification.Message, null);
        }

        await ExecuteInTransactionAsync(async () =>
        {
            attempt.MarkVerified(verification.TransactionId ?? attempt.ReferenceId ?? attempt.Id.ToString("N"), payload);
            order.MarkPaid(verification.ProviderDisplayName, verification.TransactionId);
            await _db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        await _cart.ClearAsync(cancellationToken);
        await _notifier.NotifyOrderPlacedAsync(order, cancellationToken);

        return new PaymentVerificationResponse(
            order.Id,
            order.OrderNumber,
            true,
            verification.Message ?? "پرداخت با موفقیت انجام شد.",
            verification.TransactionId);
    }

    private async Task<Order?> ResolveOrderAsync(PaymentCallbackContext context, CancellationToken cancellationToken)
    {
        if (context.OrderId.HasValue)
        {
            return await _db.Orders.FirstOrDefaultAsync(o => o.Id == context.OrderId.Value, cancellationToken);
        }

        var referenceKeys = new[] { "Authority", "authority", "trackId", "TrackId", "RefNum", "refNum", "RefId", "refId", "paymentToken", "token" };
        foreach (var key in referenceKeys)
        {
            if (!context.Parameters.TryGetValue(key, out var reference) || string.IsNullOrWhiteSpace(reference))
                continue;

            var attempt = await _db.PaymentAttempts
                .AsNoTracking()
                .Where(a => a.ProviderType == context.ProviderType && a.ReferenceId == reference)
                .OrderByDescending(a => a.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (attempt is not null)
                return await _db.Orders.FirstOrDefaultAsync(o => o.Id == attempt.OrderId, cancellationToken);
        }

        return null;
    }

    private async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            await operation();
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private PaymentProviderRuntimeSettings CreateRuntime(PaymentProviderConfigDto providerConfig) =>
        PaymentSettingsSerializer.ToRuntime(
            providerConfig.ProviderType,
            PaymentSettingsSerializer.EffectiveSandbox(providerConfig.IsSandbox, _environment.IsProduction()),
            providerConfig.Settings);
}
