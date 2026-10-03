using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Cart;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Orders;
using CMS.Modules.Shop.Application.Settings;
using CMS.Modules.Shop.Application.Shipping;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class OrderService : IOrderService
{
    private readonly ShopDbContext _db;
    private readonly ICartService _cart;
    private readonly IShopSettingsService _settings;
    private readonly IPricingEngine _pricing;
    private readonly IShippingService _shipping;
    private readonly IPaymentOrchestrator _paymentOrchestrator;
    private readonly IPaymentConfigService _paymentConfig;
    private readonly IValidator<CheckoutCommand> _checkoutValidator;
    private readonly IValidator<ChangeOrderStatusCommand> _statusValidator;
    private readonly IShopOrderNotifier _orderNotifier;

    public OrderService(
        ShopDbContext db,
        ICartService cart,
        IShopSettingsService settings,
        IPricingEngine pricing,
        IShippingService shipping,
        IPaymentOrchestrator paymentOrchestrator,
        IPaymentConfigService paymentConfig,
        IValidator<CheckoutCommand> checkoutValidator,
        IValidator<ChangeOrderStatusCommand> statusValidator,
        IShopOrderNotifier orderNotifier)
    {
        _db = db;
        _cart = cart;
        _settings = settings;
        _pricing = pricing;
        _shipping = shipping;
        _paymentOrchestrator = paymentOrchestrator;
        _paymentConfig = paymentConfig;
        _checkoutValidator = checkoutValidator;
        _statusValidator = statusValidator;
        _orderNotifier = orderNotifier;
    }

    public async Task<IReadOnlyList<OrderListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var page = await ListPagedAsync(
            new OrderListRequest { Page = 1, PageSize = PagedRequest.MaxPageSize },
            cancellationToken);
        return page.Items;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.Orders.AsNoTracking().CountAsync(cancellationToken);

    public Task<int> CountByStatusesAsync(
        IReadOnlyCollection<OrderStatus> statuses,
        CancellationToken cancellationToken = default)
    {
        if (statuses is null || statuses.Count == 0)
            return CountAsync(cancellationToken);

        return _db.Orders.AsNoTracking()
            .CountAsync(o => statuses.Contains(o.Status), cancellationToken);
    }

    public Task<PagedResult<OrderListItemDto>> ListPagedAsync(
        PagedRequest request,
        IReadOnlyCollection<OrderStatus>? statuses = null,
        CancellationToken cancellationToken = default) =>
        ListPagedAsync(
            new OrderListRequest
            {
                Page = request.Page,
                PageSize = request.PageSize,
                Search = request.Search,
                Statuses = statuses
            },
            cancellationToken);

    public async Task<PagedResult<OrderListItemDto>> ListPagedAsync(
        OrderListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(o =>
                o.OrderNumber.Contains(term) ||
                o.CustomerEmail.Contains(term) ||
                o.CustomerName.Contains(term) ||
                (o.CustomerPhone != null && o.CustomerPhone.Contains(term)) ||
                (o.ShippingCity != null && o.ShippingCity.Contains(term)) ||
                (o.CouponCode != null && o.CouponCode.Contains(term)) ||
                (o.PaymentTransactionId != null && o.PaymentTransactionId.Contains(term)));
        }

        if (request.Statuses is { Count: > 0 })
            query = query.Where(o => request.Statuses.Contains(o.Status));

        if (request.PaymentStatus.HasValue)
            query = query.Where(o => o.PaymentStatus == request.PaymentStatus.Value);

        if (request.PaymentMethod.HasValue)
            query = query.Where(o => o.PaymentMethod == request.PaymentMethod.Value);

        if (request.IsWholesale.HasValue)
            query = query.Where(o => o.IsWholesale == request.IsWholesale.Value);

        if (request.FromUtc.HasValue)
            query = query.Where(o => o.CreatedAtUtc >= request.FromUtc.Value);

        if (request.ToUtc.HasValue)
            query = query.Where(o => o.CreatedAtUtc <= request.ToUtc.Value);

        var total = await query.CountAsync(cancellationToken);
        query = ApplySort(query, request.Sort);

        var items = await query
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(o => new OrderListItemDto(
                o.Id,
                o.OrderNumber,
                o.CustomerName,
                o.CustomerEmail,
                o.Status,
                o.PaymentStatus,
                o.TotalAmount,
                o.Currency,
                o.IsWholesale,
                o.CreatedAtUtc,
                o.PaymentExpiresAtUtc,
                o.Status == OrderStatus.Pending
                    && o.PaymentStatus == PaymentStatus.Unpaid
                    && !o.StockReservationReleased))
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderListItemDto>(items, total, request.NormalizedPage, request.NormalizedPageSize);
    }

    private static IQueryable<Order> ApplySort(IQueryable<Order> query, string? sort) =>
        (sort?.Trim().ToLowerInvariant()) switch
        {
            "oldest" => query.OrderBy(o => o.CreatedAtUtc),
            "total" => query.OrderBy(o => o.TotalAmount).ThenByDescending(o => o.CreatedAtUtc),
            "total_desc" => query.OrderByDescending(o => o.TotalAmount).ThenByDescending(o => o.CreatedAtUtc),
            "status" => query.OrderBy(o => o.Status).ThenByDescending(o => o.CreatedAtUtc),
            "customer" => query.OrderBy(o => o.CustomerName).ThenByDescending(o => o.CreatedAtUtc),
            _ => query.OrderByDescending(o => o.CreatedAtUtc)
        };

    public async Task<OrderDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return order is null ? null : Map(order);
    }

    public async Task<IReadOnlyList<OrderListItemDto>> ListForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new OrderListItemDto(
                o.Id,
                o.OrderNumber,
                o.CustomerName,
                o.CustomerEmail,
                o.Status,
                o.PaymentStatus,
                o.TotalAmount,
                o.Currency,
                o.IsWholesale,
                o.CreatedAtUtc,
                o.PaymentExpiresAtUtc,
                o.Status == OrderStatus.Pending
                    && o.PaymentStatus == PaymentStatus.Unpaid
                    && !o.StockReservationReleased))
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderDetailDto?> GetForUserAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId, cancellationToken);
        return order is null ? null : Map(order);
    }

    public async Task ChangeStatusAsync(Guid id, ChangeOrderStatusCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _statusValidator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        await ExecuteInTransactionAsync(async () =>
        {
            var order = await _db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                ?? throw new NotFoundException(nameof(Order), id);

            var previousStatus = order.Status;
            order.ChangeStatus(command.Status);

            if (command.Status == OrderStatus.Cancelled && previousStatus != OrderStatus.Cancelled)
                await ReleaseReservationsAsync(order, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        var notified = await _db.Orders.AsNoTracking().Include(o => o.Lines)
            .FirstAsync(o => o.Id == id, cancellationToken);
        await _orderNotifier.NotifyOrderStatusChangedAsync(notified, command.Status, cancellationToken);
    }

    public async Task CancelForUserAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            var order = await _db.Orders.Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId, cancellationToken)
                ?? throw new NotFoundException(nameof(Order), id);

            order.CancelUnpaidReservation();
            await ReleaseReservationsAsync(order, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        var cancelled = await _db.Orders.AsNoTracking().Include(o => o.Lines)
            .FirstAsync(o => o.Id == id, cancellationToken);
        await _orderNotifier.NotifyOrderStatusChangedAsync(cancelled, OrderStatus.Cancelled, cancellationToken);
    }

    public async Task ProcessPendingPaymentMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        var now = DateTime.UtcNow;

        await ExpireDuePendingOrdersAsync(now, cancellationToken);
        await SendAbandonedPaymentRemindersAsync(settings.AbandonedPaymentReminderMinutes, now, cancellationToken);
    }

    public async Task UpdateAdminNotesAsync(Guid id, UpdateOrderAdminNotesCommand command, CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), id);

        order.SetAdminNotes(command.AdminNotes);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ConfirmBankTransferPaidAsync(
        Guid id,
        ConfirmBankTransferPaidCommand command,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            var order = await _db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                ?? throw new NotFoundException(nameof(Order), id);

            if (order.PaymentMethod != PaymentMethod.BankTransfer)
                throw new DomainException("این سفارش با واریز بانکی ثبت نشده است.");
            if (order.PaymentStatus == PaymentStatus.Paid)
                return;

            order.MarkPaid("واریز بانکی", command.TransactionReference, allowExpiredDeadline: true);
            await _db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        var notified = await _db.Orders.AsNoTracking().Include(o => o.Lines)
            .FirstAsync(o => o.Id == id, cancellationToken);
        await _orderNotifier.NotifyOrderStatusChangedAsync(notified, notified.Status, cancellationToken);
    }

    public async Task<CheckoutResultDto> CheckoutAsync(CheckoutCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _checkoutValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var settings = await _settings.GetAsync(cancellationToken);
        if (!settings.EcommerceEnabled)
            throw new DomainException("در حالت فقط‌کاتالوگ، تسویه‌حساب غیرفعال است.");

        ValidatePaymentMethod(settings, command.PaymentMethod, command.PaymentProviderConfigId);

        var cart = await _cart.GetAsync(cancellationToken);
        if (cart.Items.Count == 0)
            throw new DomainException("سبد خرید خالی است.");

        if (!string.IsNullOrWhiteSpace(cart.ValidationMessage))
            throw new DomainException(cart.ValidationMessage);

        var customerGroupId = await _pricing.ResolveCustomerGroupIdAsync(command.UserId, cancellationToken);
        var isWholesale = false;
        if (customerGroupId.HasValue)
        {
            var group = await _db.CustomerGroups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == customerGroupId.Value, cancellationToken);
            isWholesale = group?.IsWholesale ?? false;
        }

        var address = await _db.CustomerAddresses
            .FirstOrDefaultAsync(a => a.Id == command.AddressId && a.UserId == command.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerAddress), command.AddressId);

        var shippingAddressText = string.Join("، ", new[]
        {
            address.Province,
            address.City,
            address.AddressLine,
            address.PostalCode
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

        var shippingMethodId = command.ShippingMethodId ?? cart.ShippingMethodId;
        ShippingQuoteResult? shippingQuote = null;
        if (shippingMethodId.HasValue)
        {
            var totalWeight = await CalculateCartWeightAsync(cart, cancellationToken);
            shippingQuote = await _shipping.QuoteAsync(
                new ShippingQuoteRequest(
                    shippingMethodId.Value,
                    cart.Subtotal,
                    totalWeight,
                    address.Province,
                    address.City),
                cancellationToken);
        }

        Order order = null!;

        // Atomic unit: stock reservation + limited coupon redemption + order persistence
        await ExecuteInTransactionAsync(async () =>
        {
            await ReserveStockAsync(cart, cancellationToken);

            Guid? couponId = null;
            if (cart.DiscountAmount > 0 && !string.IsNullOrWhiteSpace(cart.CouponCode))
                couponId = await RedeemCouponAtomicAsync(cart.CouponCode, command.UserId, cancellationToken);

            var orderNumber = await OrderNumberGenerator.GenerateAsync(_db, cancellationToken);
            order = Order.Create(
                orderNumber,
                command.UserId,
                command.CustomerName,
                command.CustomerEmail,
                command.CustomerPhone,
                address.RecipientName,
                address.RecipientPhone,
                shippingAddressText,
                address.City,
                cart.Currency,
                command.Notes,
                isWholesale);

            order.SetPaymentMethod(command.PaymentMethod);

            foreach (var item in cart.Items)
            {
                order.AddLine(
                    item.ProductId,
                    item.VariationId,
                    item.Title,
                    item.Sku,
                    item.UnitPrice,
                    item.Quantity);
            }

            if (shippingQuote is not null)
                order.SetShipping(
                    shippingMethodId,
                    shippingQuote.Name,
                    shippingQuote.Cost,
                    shippingQuote.EstimatedDeliveryText);

            if (cart.DiscountAmount > 0)
                order.SetDiscount(cart.DiscountAmount, cart.CouponCode);

            order.SetVat(settings.EnableVat ? settings.VatPercent : null);
            order.SetPaymentDeadline(DateTime.UtcNow.AddMinutes(settings.PendingPaymentTimeoutMinutes));

            _db.Orders.Add(order);
            await _db.SaveChangesAsync(cancellationToken);

            if (couponId.HasValue)
            {
                _db.CouponRedemptions.Add(CouponRedemption.Create(
                    couponId.Value,
                    order.Id,
                    command.UserId,
                    cart.DiscountAmount));
                await _db.SaveChangesAsync(cancellationToken);
            }
        }, cancellationToken);

        var paymentSucceeded = false;
        string? paymentMessage = null;
        string? transactionId = null;
        string? redirectUrl = null;
        var awaitingOfflinePayment = false;
        BankTransferDetailsDto? bankTransfer = null;

        switch (command.PaymentMethod)
        {
            case PaymentMethod.Online:
            case PaymentMethod.SnapPay:
                var providerConfigId = await ResolvePaymentProviderConfigIdAsync(command, cancellationToken);
                var initiation = await _paymentOrchestrator.InitiateForOrderAsync(
                    order.Id,
                    providerConfigId,
                    command.CallbackBaseUrl,
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(initiation.RedirectUrl))
                {
                    redirectUrl = initiation.RedirectUrl;
                    paymentMessage = initiation.Message;
                }
                else
                {
                    paymentMessage = initiation.Message ?? "خطا در اتصال به درگاه پرداخت";
                    await ExecuteInTransactionAsync(async () =>
                    {
                        order.CancelUnpaidReservation();
                        await ReleaseReservationsAsync(order, cancellationToken);
                        await _db.SaveChangesAsync(cancellationToken);
                    }, cancellationToken);
                }

                break;

            case PaymentMethod.BankTransfer:
                awaitingOfflinePayment = true;
                bankTransfer = new BankTransferDetailsDto(
                    settings.BankAccountHolderName,
                    settings.BankName,
                    settings.BankCardNumber,
                    settings.BankShebaNumber,
                    settings.BankTransferInstructions);
                paymentMessage = string.IsNullOrWhiteSpace(settings.BankTransferInstructions)
                    ? "سفارش ثبت شد. لطفاً مبلغ را به حساب زیر واریز کنید؛ پس از تأیید ادمین، سفارش پردازش می‌شود."
                    : settings.BankTransferInstructions;
                break;

            case PaymentMethod.CashOnDelivery:
                awaitingOfflinePayment = true;
                break;

            case PaymentMethod.Invoice:
            case PaymentMethod.PayLater:
                order.MarkInvoiceRequested();
                await _db.SaveChangesAsync(cancellationToken);
                break;
        }

        if (paymentSucceeded
            || redirectUrl is not null
            || command.PaymentMethod is PaymentMethod.BankTransfer
                or PaymentMethod.CashOnDelivery
                or PaymentMethod.Invoice
                or PaymentMethod.PayLater)
        {
            if (redirectUrl is null)
            {
                await _cart.ClearAsync(cancellationToken);
                await _orderNotifier.NotifyOrderPlacedAsync(order, cancellationToken);
            }
        }

        return new CheckoutResultDto(
            order.Id,
            order.OrderNumber,
            paymentSucceeded,
            paymentMessage,
            transactionId,
            order.PaymentStatus,
            redirectUrl,
            awaitingOfflinePayment,
            bankTransfer);
    }

    private async Task ReserveStockAsync(CartDto cart, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        foreach (var item in cart.Items)
        {
            if (item.VariationId is Guid variationId)
            {
                var variation = await _db.Variations.AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == variationId && v.ProductId == item.ProductId, cancellationToken)
                    ?? throw new DomainException($"تنوع محصول «{item.Title}» یافت نشد.");

                if (variation.UnlimitedStock)
                    continue;

                var updated = await _db.Variations
                    .Where(v => v.Id == variationId
                        && !v.UnlimitedStock
                        && v.StockQuantity != null
                        && v.StockQuantity >= item.Quantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(v => v.StockQuantity, v => v.StockQuantity! - item.Quantity)
                        .SetProperty(v => v.UpdatedAtUtc, now), cancellationToken);

                if (updated == 0)
                    throw new DomainException($"موجودی «{item.Title}» کافی نیست.");
                continue;
            }

            var product = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken)
                ?? throw new DomainException($"محصول «{item.Title}» یافت نشد.");

            if (product.UnlimitedStock)
                continue;

            var productUpdated = await _db.Products
                .Where(p => p.Id == item.ProductId
                    && !p.UnlimitedStock
                    && p.StockQuantity != null
                    && p.StockQuantity >= item.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.StockQuantity, p => p.StockQuantity! - item.Quantity)
                    .SetProperty(p => p.UpdatedAtUtc, now), cancellationToken);

            if (productUpdated == 0)
                throw new DomainException($"موجودی «{item.Title}» کافی نیست.");
        }
    }

    private async Task<Guid> RedeemCouponAtomicAsync(
        string couponCode,
        string? userId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var code = couponCode.Trim().ToUpperInvariant();
        var coupon = await _db.Coupons.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == code, cancellationToken)
            ?? throw new DomainException("کد تخفیف نامعتبر است.");

        if (!coupon.IsActive
            || (coupon.StartsAtUtc.HasValue && coupon.StartsAtUtc > now)
            || (coupon.EndsAtUtc.HasValue && coupon.EndsAtUtc < now))
            throw new DomainException("کد تخفیف منقضی یا غیرفعال است.");

        if (!coupon.IsAllowedForUser(userId))
            throw new DomainException("این کد تخفیف فقط برای کاربر مشخصی قابل استفاده است.");

        var updated = await _db.Coupons
            .Where(c => c.Id == coupon.Id
                && c.IsActive
                && (c.StartsAtUtc == null || c.StartsAtUtc <= now)
                && (c.EndsAtUtc == null || c.EndsAtUtc >= now)
                && (c.MaxRedemptions == null || c.RedemptionCount < c.MaxRedemptions)
                && (c.AllowedUserId == null || c.AllowedUserId == userId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.RedemptionCount, c => c.RedemptionCount + 1)
                .SetProperty(c => c.UpdatedAtUtc, now), cancellationToken);

        if (updated == 0)
            throw new DomainException("ظرفیت استفاده از این کد تخفیف به پایان رسیده است.");

        return coupon.Id;
    }

    private async Task ReleaseReservationsAsync(Order order, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Claim release exactly once across cancel / expire / payment-fail races.
        var claimed = await _db.Orders
            .Where(o => o.Id == order.Id && !o.StockReservationReleased)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.StockReservationReleased, true)
                .SetProperty(o => o.UpdatedAtUtc, now), cancellationToken);

        if (claimed == 0)
            return;

        order.MarkStockReservationReleased();

        foreach (var line in order.Lines)
        {
            if (line.VariationId is Guid variationId)
            {
                await _db.Variations
                    .Where(v => v.Id == variationId && !v.UnlimitedStock)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(v => v.StockQuantity, v => (v.StockQuantity ?? 0) + line.Quantity)
                        .SetProperty(v => v.UpdatedAtUtc, now), cancellationToken);
            }
            else
            {
                await _db.Products
                    .Where(p => p.Id == line.ProductId && !p.UnlimitedStock)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.StockQuantity, p => (p.StockQuantity ?? 0) + line.Quantity)
                        .SetProperty(p => p.UpdatedAtUtc, now), cancellationToken);
            }
        }

        if (string.IsNullOrWhiteSpace(order.CouponCode))
            return;

        var redemptions = await _db.CouponRedemptions
            .Where(r => r.OrderId == order.Id)
            .ToListAsync(cancellationToken);

        if (redemptions.Count == 0)
            return;

        foreach (var redemption in redemptions)
        {
            await _db.Coupons
                .Where(c => c.Id == redemption.CouponId && c.RedemptionCount > 0)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.RedemptionCount, c => c.RedemptionCount - 1)
                    .SetProperty(c => c.UpdatedAtUtc, now), cancellationToken);
        }

        _db.CouponRedemptions.RemoveRange(redemptions);
    }

    private async Task ExpireDuePendingOrdersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var dueIds = await _db.Orders.AsNoTracking()
            .Where(o =>
                o.Status == OrderStatus.Pending
                && o.PaymentStatus == PaymentStatus.Unpaid
                && !o.StockReservationReleased
                && o.PaymentExpiresAtUtc != null
                && o.PaymentExpiresAtUtc <= now)
            .OrderBy(o => o.PaymentExpiresAtUtc)
            .Select(o => o.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var id in dueIds)
        {
            Order? expired = null;
            await ExecuteInTransactionAsync(async () =>
            {
                var order = await _db.Orders.Include(o => o.Lines)
                    .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
                if (order is null
                    || order.Status != OrderStatus.Pending
                    || order.PaymentStatus != PaymentStatus.Unpaid
                    || order.StockReservationReleased
                    || order.PaymentExpiresAtUtc is null
                    || order.PaymentExpiresAtUtc > now)
                {
                    return;
                }

                order.CancelUnpaidReservation();
                await ReleaseReservationsAsync(order, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                expired = order;
            }, cancellationToken);

            if (expired is not null)
                await _orderNotifier.NotifyOrderStatusChangedAsync(expired, OrderStatus.Cancelled, cancellationToken);
        }
    }

    private async Task SendAbandonedPaymentRemindersAsync(
        int reminderMinutes,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var reminderCutoff = now.AddMinutes(-reminderMinutes);
        var candidateIds = await _db.Orders.AsNoTracking()
            .Where(o =>
                o.Status == OrderStatus.Pending
                && o.PaymentStatus == PaymentStatus.Unpaid
                && !o.StockReservationReleased
                && o.PaymentReminderSentAtUtc == null
                && o.CreatedAtUtc <= reminderCutoff
                && (o.PaymentExpiresAtUtc == null || o.PaymentExpiresAtUtc > now))
            .OrderBy(o => o.CreatedAtUtc)
            .Select(o => o.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var id in candidateIds)
        {
            // Claim reminder send so concurrent workers do not duplicate SMS.
            var claimed = await _db.Orders
                .Where(o =>
                    o.Id == id
                    && o.Status == OrderStatus.Pending
                    && o.PaymentStatus == PaymentStatus.Unpaid
                    && !o.StockReservationReleased
                    && o.PaymentReminderSentAtUtc == null
                    && (o.PaymentExpiresAtUtc == null || o.PaymentExpiresAtUtc > now))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.PaymentReminderSentAtUtc, now)
                    .SetProperty(o => o.UpdatedAtUtc, now), cancellationToken);

            if (claimed == 0)
                continue;

            var order = await _db.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (order is null)
                continue;

            await _orderNotifier.NotifyAbandonedPaymentReminderAsync(order, cancellationToken);
        }
    }

    private async Task<decimal> CalculateCartWeightAsync(CartDto cart, CancellationToken cancellationToken)
    {
        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Variations)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        decimal total = 0;
        foreach (var item in cart.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                continue;

            var variation = item.VariationId.HasValue
                ? product.Variations.FirstOrDefault(v => v.Id == item.VariationId.Value)
                : null;
            total += (variation?.Weight ?? product.Weight ?? 0) * item.Quantity;
        }

        return total;
    }

    private async Task<Guid> ResolvePaymentProviderConfigIdAsync(CheckoutCommand command, CancellationToken cancellationToken)
    {
        if (command.PaymentMethod == PaymentMethod.SnapPay)
        {
            var snapPay = await _paymentConfig.GetByTypeAsync(PaymentProviderType.SnapPay, cancellationToken)
                ?? throw new DomainException("درگاه اسنپ‌پی پیکربندی نشده است.");
            if (!snapPay.IsEnabled)
                throw new DomainException("درگاه اسنپ‌پی فعال نیست.");
            return snapPay.Id;
        }

        if (!command.PaymentProviderConfigId.HasValue)
            throw new DomainException("انتخاب درگاه پرداخت الزامی است.");

        var provider = await _paymentConfig.GetAsync(command.PaymentProviderConfigId.Value, cancellationToken)
            ?? throw new DomainException("درگاه پرداخت انتخاب‌شده یافت نشد.");
        if (!provider.IsEnabled)
            throw new DomainException("درگاه پرداخت انتخاب‌شده فعال نیست.");
        if (provider.ProviderType == PaymentProviderType.SnapPay)
            throw new DomainException("برای پرداخت اسنپ‌پی، روش پرداخت اسنپ‌پی را انتخاب کنید.");

        return provider.Id;
    }

    /// <summary>
    /// User transactions must run inside the EF execution strategy when EnableRetryOnFailure is on.
    /// </summary>
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

    private static void ValidatePaymentMethod(ShopSettingsDto settings, PaymentMethod method, Guid? paymentProviderConfigId)
    {
        var allowed = method switch
        {
            PaymentMethod.Online => settings.EnableOnlinePayment,
            PaymentMethod.BankTransfer => settings.EnableBankTransfer,
            PaymentMethod.CashOnDelivery => false,
            PaymentMethod.Invoice or PaymentMethod.PayLater => settings.EnableWholesaleInvoice,
            PaymentMethod.SnapPay => true,
            _ => false
        };

        if (!allowed)
            throw new DomainException("روش پرداخت انتخاب‌شده فعال نیست.");

        if (method == PaymentMethod.Online && !paymentProviderConfigId.HasValue)
            throw new DomainException("انتخاب درگاه پرداخت الزامی است.");

        if (method == PaymentMethod.BankTransfer && !settings.HasBankTransferAccount)
            throw new DomainException("اطلاعات حساب بانکی فروشگاه هنوز تنظیم نشده است.");
    }

    private static OrderDetailDto Map(Order order) =>
        new(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.PaymentStatus,
            order.PaymentMethod,
            order.UserId,
            order.CustomerName,
            order.CustomerEmail,
            order.CustomerPhone,
            order.RecipientName,
            order.RecipientPhone,
            order.ShippingAddress,
            order.ShippingCity,
            order.ShippingMethodName,
            order.ShippingEstimatedDelivery,
            order.SubtotalAmount,
            order.ShippingAmount,
            order.DiscountAmount,
            order.CouponCode,
            order.VatAmount,
            order.VatPercent,
            order.Currency,
            order.TotalAmount,
            order.PaymentProvider,
            order.PaymentTransactionId,
            order.Notes,
            order.AdminNotes,
            order.IsWholesale,
            order.CreatedAtUtc,
            order.PaymentExpiresAtUtc,
            order.IsUnpaidPending,
            order.Lines.Select(l => new OrderLineDto(
                l.ProductId,
                l.VariationId,
                l.ProductTitle,
                l.Sku,
                l.UnitPrice,
                l.Quantity,
                l.LineTotal)).ToList());
}
