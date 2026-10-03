using CMS.Application.Account;
using CMS.Application.Auth;
using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Account;
using CMS.Modules.Shop.Application.Cart;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Orders;
using CMS.Modules.Shop.Application.Payments;
using CMS.Modules.Shop.Application.Settings;
using CMS.Modules.Shop.Application.Shipping;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Web;
using CMS.Modules.Shop.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;
using System.Security.Claims;

namespace CMS.Modules.Shop.Web.Controllers;

[Route("shop")]
public class CartController : Controller
{
    private readonly ICartService _cart;
    private readonly IOrderService _orders;
    private readonly IShopSettingsService _settings;
    private readonly IShippingService _shipping;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopPublic> _localizer;
    private readonly ICustomerProfileService _profiles;
    private readonly ICustomerAddressService _addresses;
    private readonly IPaymentConfigService _paymentConfig;

    public CartController(
        ICartService cart,
        IOrderService orders,
        IShopSettingsService settings,
        IShippingService shipping,
        IFeatureManager features,
        IStringLocalizer<ShopPublic> localizer,
        ICustomerProfileService profiles,
        ICustomerAddressService addresses,
        IPaymentConfigService paymentConfig)
    {
        _cart = cart;
        _orders = orders;
        _settings = settings;
        _shipping = shipping;
        _features = features;
        _localizer = localizer;
        _profiles = profiles;
        _addresses = addresses;
        _paymentConfig = paymentConfig;
    }

    [HttpGet("cart")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var settings = await _settings.GetAsync(cancellationToken);
        if (!settings.EcommerceEnabled)
            return NotFound();

        ViewData["Title"] = _localizer["Cart"].Value;
        var cart = await _cart.GetAsync(cancellationToken);
        return View(new CartPageViewModel { Cart = cart, EcommerceEnabled = true, CouponCode = cart.CouponCode });
    }

    [HttpPost("cart/add")]
    public async Task<IActionResult> Add(Guid productId, Guid? variationId, int quantity, string? returnUrl, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        try
        {
            await _cart.AddAsync(new AddToCartCommand(productId, variationId, quantity <= 0 ? 1 : quantity), cancellationToken);
            TempData["Success"] = _localizer["AddedToCart"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cart/update")]
    public async Task<IActionResult> Update(Guid productId, Guid? variationId, int quantity, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        try
        {
            await _cart.UpdateAsync(new UpdateCartItemCommand(productId, variationId, quantity), cancellationToken);
            TempData["Success"] = _localizer["CartUpdated"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cart/remove")]
    public async Task<IActionResult> Remove(Guid productId, Guid? variationId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        await _cart.RemoveAsync(productId, variationId, cancellationToken);
        TempData["Success"] = _localizer["ItemRemoved"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cart/coupon")]
    public async Task<IActionResult> ApplyCoupon(string? couponCode, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        try
        {
            await _cart.ApplyCouponAsync(new ApplyCouponCommand(couponCode), cancellationToken);
            TempData["Success"] = _localizer["CouponApplied"].Value;
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cart/shipping")]
    public async Task<IActionResult> SetShipping(Guid? shippingMethodId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        try
        {
            await _cart.SetShippingAsync(new SetCartShippingCommand(shippingMethodId), cancellationToken);
            TempData["Success"] = _localizer["ShippingUpdated"].Value;
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("checkout")]
    [Authorize(Policy = CustomerAuthDefaults.PolicyName)]
    public async Task<IActionResult> Checkout(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var settings = await _settings.GetAsync(cancellationToken);
        if (!settings.EcommerceEnabled)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge(authenticationSchemes: CustomerAuthDefaults.AuthenticationScheme);

        var profile = await _profiles.GetAsync(userId, cancellationToken);
        if (profile is null || !profile.IsProfileComplete)
        {
            TempData["Error"] = "برای ثبت سفارش ابتدا پروفایل خود را کامل کنید.";
            return RedirectToAction("Profile", "Account");
        }

        var cart = await _cart.GetAsync(cancellationToken);
        if (cart.Items.Count == 0)
            return RedirectToAction(nameof(Index));

        ViewData["Title"] = _localizer["Checkout"].Value;
        return View(await BuildCheckoutFormAsync(cart, settings, userId, cancellationToken));
    }

    [HttpPost("checkout")]
    [Authorize(Policy = CustomerAuthDefaults.PolicyName)]
    public async Task<IActionResult> Checkout(CheckoutFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var settings = await _settings.GetAsync(cancellationToken);
        if (!settings.EcommerceEnabled)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge(authenticationSchemes: CustomerAuthDefaults.AuthenticationScheme);

        var profile = await _profiles.GetAsync(userId, cancellationToken);
        if (profile is null || !profile.IsProfileComplete)
        {
            TempData["Error"] = "برای ثبت سفارش ابتدا پروفایل خود را کامل کنید.";
            return RedirectToAction("Profile", "Account");
        }

        model.Cart = await _cart.GetAsync(cancellationToken);
        model = await BuildCheckoutFormAsync(model.Cart, settings, userId, cancellationToken, model);
        ViewData["Title"] = _localizer["Checkout"].Value;
        if (model.Cart.Items.Count == 0)
            return RedirectToAction(nameof(Index));

        if (!model.AddressId.HasValue)
            ModelState.AddModelError(nameof(model.AddressId), "انتخاب آدرس الزامی است.");

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var email = string.IsNullOrWhiteSpace(profile.Email)
                ? $"{profile.Phone}@customers.local"
                : profile.Email;

            var callbackBase = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var result = await _orders.CheckoutAsync(
                new CheckoutCommand(
                    profile.FullName ?? profile.Phone,
                    email,
                    profile.Phone,
                    model.AddressId!.Value,
                    model.Notes,
                    model.PaymentMethod,
                    model.ShippingMethodId,
                    model.PaymentProviderConfigId,
                    userId,
                    callbackBase),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(result.RedirectUrl))
                return Redirect(result.RedirectUrl);

            return View("CheckoutResult", new CheckoutResultViewModel
            {
                OrderId = result.OrderId,
                OrderNumber = result.OrderNumber,
                PaymentSucceeded = result.PaymentSucceeded,
                AwaitingOfflinePayment = result.AwaitingOfflinePayment,
                PaymentMessage = result.PaymentMessage,
                TransactionId = result.TransactionId,
                BankTransfer = result.BankTransfer
            });
        }
        catch (ValidationException ex)
        {
            foreach (var (key, messages) in ex.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(key, message);
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private async Task<CheckoutFormViewModel> BuildCheckoutFormAsync(
        CartDto cart,
        CMS.Modules.Shop.Application.Settings.ShopSettingsDto settings,
        string userId,
        CancellationToken cancellationToken,
        CheckoutFormViewModel? model = null)
    {
        model ??= new CheckoutFormViewModel();
        model.Cart = cart;

        var profile = await _profiles.GetAsync(userId, cancellationToken);
        model.ProfileComplete = profile?.IsProfileComplete == true;
        model.ProfileName = profile?.FullName;
        model.ProfilePhone = profile?.Phone;
        model.ProfileEmail = profile?.Email;

        model.Addresses = await _addresses.ListAsync(userId, cancellationToken);
        if (!model.AddressId.HasValue)
            model.AddressId = model.Addresses.FirstOrDefault(a => a.IsDefault)?.Id
                ?? model.Addresses.FirstOrDefault()?.Id;

        var methods = await _shipping.ListAsync(cancellationToken);
        model.ShippingMethods = methods.Where(m => m.IsActive).ToList();

        var selectedAddress = model.Addresses.FirstOrDefault(a => a.Id == model.AddressId);
        var city = selectedAddress?.City;
        var province = selectedAddress?.Province;
        var totalWeight = await _cart.GetWeightAsync(cancellationToken);
        var shippingCosts = new Dictionary<Guid, decimal>();
        foreach (var method in model.ShippingMethods)
        {
            var quote = await _shipping.QuoteAsync(
                new ShippingQuoteRequest(method.Id, cart.Subtotal, totalWeight, province, city),
                cancellationToken);
            shippingCosts[method.Id] = quote?.Cost ?? method.FixedCost;
        }

        model.ShippingCosts = shippingCosts;
        model.ShippingMethodOptions = model.ShippingMethods
            .Select(m =>
            {
                var cost = shippingCosts.GetValueOrDefault(m.Id, m.FixedCost);
                var label = $"{m.Name} — {cost:N0}";
                if (!string.IsNullOrWhiteSpace(m.EstimatedDeliveryText))
                    label += $" ({m.EstimatedDeliveryText})";
                return new SelectListItem(label, m.Id.ToString(), m.Id == model.ShippingMethodId);
            })
            .ToList();

        var paymentOptions = new List<SelectListItem>();
        if (settings.EnableOnlinePayment)
            paymentOptions.Add(new SelectListItem(_localizer["PaymentOnline"].Value, PaymentMethod.Online.ToString(), model.PaymentMethod == PaymentMethod.Online));
        if (settings.EnableBankTransfer && settings.HasBankTransferAccount)
            paymentOptions.Add(new SelectListItem(_localizer["PaymentBankTransfer"].Value, PaymentMethod.BankTransfer.ToString(), model.PaymentMethod == PaymentMethod.BankTransfer));

        var enabledProviders = await _paymentConfig.ListEnabledAsync(cancellationToken);
        model.OnlinePaymentProviders = enabledProviders
            .Where(p => p.ProviderType != PaymentProviderType.SnapPay)
            .ToList();
        model.SnapPayEnabled = enabledProviders.Any(p => p.ProviderType == PaymentProviderType.SnapPay);
        if (model.SnapPayEnabled)
            paymentOptions.Add(new SelectListItem("اسنپ‌پی", PaymentMethod.SnapPay.ToString(), model.PaymentMethod == PaymentMethod.SnapPay));

        if (!model.PaymentProviderConfigId.HasValue)
            model.PaymentProviderConfigId = model.OnlinePaymentProviders.FirstOrDefault()?.Id;

        model.PaymentMethodOptions = paymentOptions;

        return model;
    }
}
