using System.Security.Claims;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Cart;
using CMS.Modules.Shop.Domain.Commerce;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Pricing;
using CMS.Modules.Shop.Application.Settings;
using CMS.Modules.Shop.Application.Shipping;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class CartService : ICartService
{
    private const string SessionCartIdKey = "shop.cart.sid";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ShopDbContext _db;
    private readonly IShopSettingsService _settings;
    private readonly IPricingEngine _pricing;
    private readonly IShippingService _shipping;

    public CartService(
        IHttpContextAccessor httpContextAccessor,
        ShopDbContext db,
        IShopSettingsService settings,
        IPricingEngine pricing,
        IShippingService shipping)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
        _settings = settings;
        _pricing = pricing;
        _shipping = shipping;
    }

    public async Task<CartDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(cancellationToken);
        return await BuildCartAsync(cart, cancellationToken);
    }

    public async Task<int> GetItemCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            return await _db.Carts.AsNoTracking()
                .Where(c => c.UserId == userId)
                .SelectMany(c => c.Items)
                .SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0;
        }

        var context = _httpContextAccessor.HttpContext;
        if (context?.Session is null)
            return 0;

        await context.Session.LoadAsync(cancellationToken);
        var sessionId = context.Session.GetString(SessionCartIdKey);
        if (string.IsNullOrWhiteSpace(sessionId))
            return 0;

        return await _db.Carts.AsNoTracking()
            .Where(c => c.SessionId == sessionId)
            .SelectMany(c => c.Items)
            .SumAsync(i => (int?)i.Quantity, cancellationToken) ?? 0;
    }

    public async Task AddAsync(AddToCartCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Quantity <= 0)
            throw new DomainException("تعداد باید مثبت باشد.");

        var cart = await GetOrCreateCartAsync(cancellationToken);
        var existingQty = cart.Items
            .FirstOrDefault(i => i.ProductId == command.ProductId && i.VariationId == command.VariationId)
            ?.Quantity ?? 0;

        await EnsureCanPurchaseAsync(
            command.ProductId,
            command.VariationId,
            existingQty + command.Quantity,
            cancellationToken);

        var knownItemIds = cart.Items.Select(i => i.Id).ToHashSet();
        var item = cart.UpsertItem(command.ProductId, command.VariationId, command.Quantity);

        // BaseEntity assigns Guid.NewGuid() up front; EF would treat the new line as Modified/UPDATE
        // and throw DbUpdateConcurrencyException unless we force Added.
        if (!knownItemIds.Contains(item.Id))
            _db.Entry(item).State = EntityState.Added;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UpdateCartItemCommand command, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(cancellationToken);
        var existing = cart.Items.FirstOrDefault(i =>
            i.ProductId == command.ProductId && i.VariationId == command.VariationId);

        if (existing is null)
            throw new NotFoundException("CartItem", command.ProductId);

        if (command.Quantity <= 0)
        {
            cart.RemoveItem(command.ProductId, command.VariationId);
            _db.CartItems.Remove(existing);
        }
        else
        {
            await EnsureCanPurchaseAsync(command.ProductId, command.VariationId, command.Quantity, cancellationToken);
            cart.SetItemQuantity(command.ProductId, command.VariationId, command.Quantity);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid productId, Guid? variationId = null, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(cancellationToken);
        var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId && i.VariationId == variationId);
        cart.RemoveItem(productId, variationId);
        if (existing is not null)
            _db.CartItems.Remove(existing);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(cancellationToken);
        var items = cart.Items.ToList();
        cart.Clear();
        if (items.Count > 0)
            _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyCouponAsync(ApplyCouponCommand command, CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        if (!settings.EnableCoupons)
            throw new DomainException("کوپن‌ها غیرفعال هستند.");

        var cart = await GetOrCreateCartAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(command.Code))
        {
            cart.ApplyCoupon(null);
        }
        else
        {
            var code = command.Code.Trim().ToUpperInvariant();
            var coupon = await _db.Coupons.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == code, cancellationToken)
                ?? throw new DomainException("کوپن نامعتبر است.");

            if (!coupon.IsEffective(DateTime.UtcNow))
                throw new DomainException("کوپن منقضی یا غیرفعال است.");

            if (!coupon.IsAllowedForUser(GetUserId()))
                throw new DomainException("این کد تخفیف فقط برای کاربر مشخصی قابل استفاده است.");

            if (coupon.MinOrderAmount.HasValue)
            {
                var subtotal = await EstimateSubtotalAsync(cart, cancellationToken);
                if (subtotal < coupon.MinOrderAmount.Value)
                    throw new DomainException(
                        $"حداقل مبلغ خرید برای این کد تخفیف {coupon.MinOrderAmount.Value:N0} است.");
            }

            cart.ApplyCoupon(code);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetShippingAsync(SetCartShippingCommand command, CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(cancellationToken);
        if (command.ShippingMethodId.HasValue)
        {
            var exists = await _db.ShippingMethods.AnyAsync(
                m => m.Id == command.ShippingMethodId.Value && m.IsActive,
                cancellationToken);
            if (!exists)
                throw new NotFoundException(nameof(ShippingMethod), command.ShippingMethodId.Value);
        }

        cart.SetShippingMethod(command.ShippingMethodId);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ShoppingCart> GetOrCreateCartAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            var cart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

            if (cart is not null)
                return cart;

            cart = ShoppingCart.CreateForUser(userId);
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync(cancellationToken);
            return cart;
        }

        var sessionId = EnsureSessionId();
        var sessionCart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId, cancellationToken);

        if (sessionCart is not null)
            return sessionCart;

        sessionCart = ShoppingCart.CreateForSession(sessionId);
        _db.Carts.Add(sessionCart);
        await _db.SaveChangesAsync(cancellationToken);
        return sessionCart;
    }

    private async Task<CartDto> BuildCartAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        if (cart.Items.Count == 0)
            return new CartDto([], "IRR", 0, 0, 0, 0, null, 0, null, null, null);

        var settings = await _settings.GetAsync(cancellationToken);
        var userId = GetUserId();
        var customerGroupId = await _pricing.ResolveCustomerGroupIdAsync(userId, cancellationToken);
        CustomerGroup? customerGroup = null;
        if (customerGroupId.HasValue)
        {
            customerGroup = await _db.CustomerGroups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == customerGroupId.Value, cancellationToken);
        }

        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Variations)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var quoteRequests = cart.Items
            .Where(line => products.ContainsKey(line.ProductId))
            .Select(line => new PriceQuoteRequest(line.ProductId, line.VariationId, line.Quantity, customerGroupId))
            .ToList();
        var quotes = await _pricing.QuoteManyAsync(quoteRequests, cancellationToken);

        var items = new List<CartItemDto>();
        var couponLines = new List<CouponLineContext>();
        decimal subtotal = 0;
        decimal totalWeight = 0;
        string? validationMessage = null;

        foreach (var line in cart.Items)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                continue;

            if (!quotes.TryGetValue((line.ProductId, line.VariationId), out var quote))
                continue;

            ProductVariation? variation = null;
            if (line.VariationId.HasValue)
                variation = product.Variations.FirstOrDefault(v => v.Id == line.VariationId.Value);

            subtotal += quote.LineTotal;
            totalWeight += (variation?.Weight ?? product.Weight ?? 0) * line.Quantity;

            items.Add(new CartItemDto(
                product.Id,
                line.VariationId,
                product.Title,
                product.Slug,
                variation?.Sku ?? product.Sku,
                quote.UnitPrice,
                product.Currency,
                line.Quantity,
                variation?.ImageUrl ?? product.CoverImageUrl,
                ResolveMaxQuantity(product, variation)));

            couponLines.Add(new CouponLineContext(
                product.Id,
                product.CategoryId,
                line.Quantity,
                quote.LineTotal));
        }

        var currency = items.FirstOrDefault()?.Currency ?? "IRR";
        decimal discountAmount = 0;
        Coupon? coupon = null;

        if (!string.IsNullOrWhiteSpace(cart.CouponCode) && settings.EnableCoupons)
        {
            coupon = await _db.Coupons.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == cart.CouponCode, cancellationToken);
            if (coupon is not null && coupon.IsAllowedForUser(userId))
                discountAmount = CouponService.CalculateDiscount(coupon, couponLines, subtotal);
        }

        decimal shippingAmount = 0;
        if (cart.ShippingMethodId.HasValue)
        {
            var quote = await _shipping.QuoteAsync(
                new ShippingQuoteRequest(cart.ShippingMethodId.Value, subtotal, totalWeight, null, null),
                cancellationToken);
            shippingAmount = quote?.Cost ?? 0;
        }

        var taxableBase = Math.Max(0, subtotal - discountAmount + shippingAmount);
        decimal vatAmount = 0;
        decimal? vatPercent = settings.EnableVat ? settings.VatPercent : null;
        if (vatPercent is > 0)
            vatAmount = VatCalculator.ComputeAmount(taxableBase, vatPercent.Value);

        var total = taxableBase + vatAmount;
        validationMessage = ValidateWholesaleMinimums(settings, customerGroup, items, products, subtotal);

        return new CartDto(
            items,
            currency,
            subtotal,
            discountAmount,
            shippingAmount,
            vatAmount,
            vatPercent,
            total,
            cart.CouponCode,
            cart.ShippingMethodId,
            validationMessage);
    }

    private static string? ValidateWholesaleMinimums(
        ShopSettingsDto settings,
        CustomerGroup? customerGroup,
        IReadOnlyList<CartItemDto> items,
        IReadOnlyDictionary<Guid, Product> products,
        decimal subtotal)
    {
        if (customerGroup?.IsWholesale != true)
            return null;

        foreach (var item in items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                continue;

            if (product.WholesaleMinimumOrderQuantity is int minProductQty && item.Quantity < minProductQty)
                return $"حداقل تعداد عمده برای «{product.Title}» {minProductQty} عدد است.";

            if (product.WholesaleMinimumOrderAmount is decimal minProductAmount && item.LineTotal < minProductAmount)
                return $"حداقل مبلغ عمده برای «{product.Title}» {minProductAmount:N0} است.";
        }

        var minQty = customerGroup.MinimumOrderQuantity ?? settings.WholesaleMinimumOrderQuantity;
        var minAmount = customerGroup.MinimumOrderAmount ?? settings.WholesaleMinimumOrderAmount;

        if (minQty.HasValue && items.Sum(i => i.Quantity) < minQty.Value)
            return $"حداقل تعداد سفارش عمده {minQty.Value} عدد است.";

        if (minAmount.HasValue && subtotal < minAmount.Value)
            return $"حداقل مبلغ سفارش عمده {minAmount.Value:N0} است.";

        return null;
    }

    private async Task<decimal> EstimateSubtotalAsync(ShoppingCart cart, CancellationToken cancellationToken)
    {
        if (cart.Items.Count == 0)
            return 0;

        var userId = GetUserId();
        var customerGroupId = await _pricing.ResolveCustomerGroupIdAsync(userId, cancellationToken);
        var requests = cart.Items
            .Select(line => new PriceQuoteRequest(line.ProductId, line.VariationId, line.Quantity, customerGroupId))
            .ToList();
        var quotes = await _pricing.QuoteManyAsync(requests, cancellationToken);
        return quotes.Values.Sum(q => q.LineTotal);
    }

    private async Task EnsureCanPurchaseAsync(
        Guid productId,
        Guid? variationId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        if (!settings.EcommerceEnabled)
            throw new DomainException("در حال حاضر فروش آنلاین فعال نیست.");

        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Variations)
            .FirstOrDefaultAsync(p => p.Id == productId && p.Status == ProductStatus.Active, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), productId);

        if (!product.IsAvailable)
            throw new DomainException("محصول در دسترس نیست.");

        if (!settings.Mode.CanBuy(product.IsPurchasable))
            throw new DomainException("این محصول در حالت فعلی فروشگاه قابل خرید نیست.");

        ProductVariation? variation = null;
        if (variationId.HasValue)
        {
            variation = product.Variations.FirstOrDefault(v => v.Id == variationId.Value)
                ?? throw new NotFoundException(nameof(ProductVariation), variationId.Value);
            if (variation.Status != VariationStatus.Active)
                throw new DomainException("تنوع انتخاب‌شده در دسترس نیست.");
        }

        EnsureQuantityWithinStock(product, variation, quantity);

        var userId = GetUserId();
        var customerGroupId = await _pricing.ResolveCustomerGroupIdAsync(userId, cancellationToken);
        CustomerGroup? group = null;
        if (customerGroupId.HasValue)
        {
            group = await _db.CustomerGroups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == customerGroupId.Value, cancellationToken);
        }

        var minQty = product.MinimumOrderQuantity;
        if (group?.IsWholesale == true)
        {
            var wholesaleMin = product.WholesaleMinimumOrderQuantity
                ?? group.MinimumOrderQuantity
                ?? settings.WholesaleMinimumOrderQuantity
                ?? 1;
            minQty = Math.Max(minQty, wholesaleMin);
        }

        if (quantity < minQty)
            throw new DomainException($"حداقل تعداد سفارش {minQty} عدد است.");
    }

    private static void EnsureQuantityWithinStock(Product product, ProductVariation? variation, int quantity)
    {
        var unlimited = variation?.UnlimitedStock ?? product.UnlimitedStock;
        if (unlimited)
            return;

        var available = variation?.StockQuantity ?? product.StockQuantity ?? 0;
        if (available <= 0)
            throw new DomainException("موجودی این محصول به اتمام رسیده است.");

        if (quantity > available)
            throw new DomainException($"حداکثر تعداد قابل سفارش {available} عدد است.");
    }

    private static int? ResolveMaxQuantity(Product product, ProductVariation? variation)
    {
        var unlimited = variation?.UnlimitedStock ?? product.UnlimitedStock;
        if (unlimited)
            return null;

        return variation?.StockQuantity ?? product.StockQuantity ?? 0;
    }

    private string? GetUserId() =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    private string EnsureSessionId()
    {
        var session = GetSession();
        var sessionId = session.GetString(SessionCartIdKey);
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = Guid.NewGuid().ToString("N");
            session.SetString(SessionCartIdKey, sessionId);
        }

        return sessionId;
    }

    private ISession GetSession()
    {
        var context = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is required for cart.");
        return context.Session;
    }

    public async Task<decimal> GetWeightAsync(CancellationToken cancellationToken = default)
    {
        var cart = await GetOrCreateCartAsync(cancellationToken);
        if (cart.Items.Count == 0)
            return 0;

        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Variations)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        decimal total = 0;
        foreach (var line in cart.Items)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                continue;

            var variation = line.VariationId.HasValue
                ? product.Variations.FirstOrDefault(v => v.Id == line.VariationId.Value)
                : null;
            total += (variation?.Weight ?? product.Weight ?? 0) * line.Quantity;
        }

        return total;
    }
}

file static class ShopModePurchaseExtensions
{
    public static bool CanBuy(this ShopMode mode, bool isPurchasable) =>
        mode switch
        {
            ShopMode.OnlineStore => true,
            ShopMode.Hybrid => isPurchasable,
            _ => false
        };
}
