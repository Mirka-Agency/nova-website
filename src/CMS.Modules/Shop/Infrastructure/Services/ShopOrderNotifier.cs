using CMS.Application.Background;
using CMS.Application.Email;
using CMS.Application.Sms;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMS.Modules.Shop.Infrastructure.Services;

public interface IShopOrderNotifier
{
    Task NotifyOrderPlacedAsync(Order order, CancellationToken cancellationToken = default);
    Task NotifyOrderStatusChangedAsync(Order order, OrderStatus status, CancellationToken cancellationToken = default);
    Task NotifyAbandonedPaymentReminderAsync(Order order, CancellationToken cancellationToken = default);
}

public sealed class ShopOrderNotifier : IShopOrderNotifier
{
    private readonly IBackgroundTaskQueue _queue;

    public ShopOrderNotifier(IBackgroundTaskQueue queue)
    {
        _queue = queue;
    }

    public Task NotifyOrderPlacedAsync(Order order, CancellationToken cancellationToken = default)
    {
        var orderId = order.Id;
        return _queue.QueueAsync(async (sp, ct) =>
        {
            var db = sp.GetRequiredService<ShopDbContext>();
            var loaded = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (loaded is null)
                return;

            var sms = sp.GetRequiredService<ISmsSender>();
            var admins = sp.GetRequiredService<IAdminNotifier>();
            var logger = sp.GetRequiredService<ILogger<ShopOrderNotifier>>();

            var customerPhone = SmsPhoneNormalizer.Normalize(loaded.CustomerPhone ?? loaded.RecipientPhone);
            if (!string.IsNullOrWhiteSpace(customerPhone))
            {
                var customerText =
                    $"سفارش شما با شماره {loaded.OrderNumber} ثبت شد. مبلغ: {loaded.TotalAmount:N0} {loaded.Currency}";
                if (loaded.PaymentMethod == PaymentMethod.BankTransfer)
                {
                    customerText +=
                        ". لطفاً پس از انتقال وجه، فیش را به پشتیبانی ارسال کنید.";
                }

                var result = await sms.SendAsync(new SmsMessage(customerPhone, customerText), ct);
                if (!result.Succeeded && !result.Skipped)
                    logger.LogWarning("Order placed SMS to customer failed: {Reason}", result.ProviderMessage);
            }

            var adminMessage =
                $"سفارش جدید ثبت شد.\n" +
                $"شماره: {loaded.OrderNumber}\n" +
                $"مبلغ: {loaded.TotalAmount:N0} {loaded.Currency}\n" +
                $"مشتری: {loaded.CustomerName}\n" +
                $"موبایل: {loaded.CustomerPhone ?? loaded.RecipientPhone}\n" +
                $"تحویل‌گیرنده: {loaded.RecipientName} — {loaded.RecipientPhone}\n" +
                $"آدرس: {loaded.ShippingAddress}";

            await admins.NotifyAdminsAsync($"سفارش جدید {loaded.OrderNumber}", adminMessage, ct);
        }, cancellationToken).AsTask();
    }

    public Task NotifyOrderStatusChangedAsync(Order order, OrderStatus status, CancellationToken cancellationToken = default)
    {
        var orderId = order.Id;
        return _queue.QueueAsync(async (sp, ct) =>
        {
            var db = sp.GetRequiredService<ShopDbContext>();
            var loaded = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (loaded is null)
                return;

            var customerPhone = SmsPhoneNormalizer.Normalize(loaded.CustomerPhone ?? loaded.RecipientPhone);
            if (string.IsNullOrWhiteSpace(customerPhone))
                return;

            var statusLabel = status switch
            {
                OrderStatus.Pending => "در انتظار",
                OrderStatus.Reviewing => "در حال بررسی",
                OrderStatus.Approved => "تأیید شده",
                OrderStatus.Processing => "در حال پردازش",
                OrderStatus.ReadyForShipping => "آماده ارسال",
                OrderStatus.Shipped => "ارسال شده",
                OrderStatus.Completed => "تکمیل شده",
                OrderStatus.Cancelled => "لغو شده",
                _ => status.ToString()
            };

            var sms = sp.GetRequiredService<ISmsSender>();
            var logger = sp.GetRequiredService<ILogger<ShopOrderNotifier>>();
            var text = $"وضعیت سفارش {loaded.OrderNumber} به «{statusLabel}» تغییر کرد.";
            var result = await sms.SendAsync(new SmsMessage(customerPhone, text), ct);
            if (!result.Succeeded && !result.Skipped)
                logger.LogWarning("Order status SMS failed: {Reason}", result.ProviderMessage);
        }, cancellationToken).AsTask();
    }

    public Task NotifyAbandonedPaymentReminderAsync(Order order, CancellationToken cancellationToken = default)
    {
        var orderId = order.Id;
        return _queue.QueueAsync(async (sp, ct) =>
        {
            var db = sp.GetRequiredService<ShopDbContext>();
            var loaded = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (loaded is null)
                return;

            var phone = SmsPhoneNormalizer.Normalize(loaded.CustomerPhone ?? loaded.RecipientPhone);
            if (string.IsNullOrWhiteSpace(phone))
                return;

            var sms = sp.GetRequiredService<ISmsSender>();
            var logger = sp.GetRequiredService<ILogger<ShopOrderNotifier>>();
            var text =
                $"سفارش {loaded.OrderNumber} هنوز پرداخت نشده است. لطفاً هرچه سریع‌تر اقدام به پرداخت کنید تا سفارش لغو نشود.";
            var result = await sms.SendAsync(new SmsMessage(phone, text), ct);
            if (!result.Succeeded && !result.Skipped)
                logger.LogWarning("Abandoned payment reminder SMS failed: {Reason}", result.ProviderMessage);
        }, cancellationToken).AsTask();
    }
}
