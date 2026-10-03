using System.Security.Claims;
using CMS.Application.Account;
using CMS.Application.Auth;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Account;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Locations;
using CMS.Web.Security;
using CMS.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CMS.Web.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly ICustomerAuthService _auth;
    private readonly ICustomerProfileService _profiles;
    private readonly ICustomerAddressService _addresses;
    private readonly IOrderService _orders;
    private readonly IIranLocationService _locations;

    public AccountController(
        ICustomerAuthService auth,
        ICustomerProfileService profiles,
        ICustomerAddressService addresses,
        IOrderService orders,
        IIranLocationService locations)
    {
        _auth = auth;
        _profiles = profiles;
        _addresses = addresses;
        _orders = orders;
        _locations = locations;
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true &&
            User.Identity.AuthenticationType == CustomerAuthDefaults.AuthenticationScheme)
            return RedirectToAction(nameof(Index));

        return View(new CustomerLoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("login/request-otp")]
    [EnableRateLimiting(RateLimitingConfiguration.AuthPolicy)]
    public async Task<IActionResult> RequestOtp(CustomerLoginViewModel model, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.Phone))
        {
            ModelState.AddModelError(nameof(model.Phone), "شماره موبایل الزامی است.");
            return View("Login", model);
        }

        try
        {
            var result = await _auth.RequestOtpAsync(model.Phone, cancellationToken);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "ارسال کد ناموفق بود.");
                model.RetryAfterSeconds = result.RetryAfterSeconds;
                return View("Login", model);
            }

            model.Step = "otp";
            model.RetryAfterSeconds = result.RetryAfterSeconds ?? 120;
            TempData["Success"] = "کد تأیید ارسال شد.";
            return View("Login", model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Login", model);
        }
    }

    [HttpPost("login/verify-otp")]
    [EnableRateLimiting(RateLimitingConfiguration.AuthPolicy)]
    public async Task<IActionResult> VerifyOtp(CustomerLoginViewModel model, CancellationToken cancellationToken)
    {
        model.Step = "otp";
        if (string.IsNullOrWhiteSpace(model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "کد تأیید الزامی است.");
            return View("Login", model);
        }

        try
        {
            var result = await _auth.VerifyOtpAsync(model.Phone, model.Code, cancellationToken);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "ورود ناموفق بود.");
                return View("Login", model);
            }

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Login", model);
        }
    }

    [HttpPost("logout")]
    [Authorize(Policy = CustomerAuthDefaults.PolicyName)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _auth.SignOutAsync(cancellationToken);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("")]
    [HttpGet("index")]
    [Authorize(Policy = CustomerAuthDefaults.PolicyName)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var profile = await _profiles.GetAsync(userId, cancellationToken);
        ViewData["Title"] = "حساب کاربری";
        return View(profile);
    }

    [HttpGet("profile")]
    [Authorize(Policy = CustomerAuthDefaults.PolicyName)]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var profile = await _profiles.GetAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);
        ViewData["Title"] = "پروفایل";
        return View(new CustomerProfileFormViewModel
        {
            FullName = profile.FullName ?? string.Empty,
            Email = profile.Email,
            Phone = profile.Phone
        });
    }

    [HttpPost("profile")]
    [Authorize(Policy = CustomerAuthDefaults.PolicyName)]
    public async Task<IActionResult> Profile(CustomerProfileFormViewModel model, CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        model.Phone = (await _profiles.GetAsync(userId, cancellationToken))?.Phone ?? string.Empty;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _profiles.UpdateAsync(userId, new UpdateCustomerProfileCommand(model.FullName, model.Email), cancellationToken);
            TempData["Success"] = "پروفایل ذخیره شد.";
            return RedirectToAction(nameof(Profile));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet("orders")]
    [Authorize(Policy = CustomerAuthDefaults.PolicyName)]
    public async Task<IActionResult> Orders(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "سفارش‌ها";
        return View(await _orders.ListForUserAsync(RequireUserId(), cancellationToken));
    }

    [HttpGet("orders/{id:guid}")]
    [Authorize(Policy = CustomerAuthDefaults.PolicyName)]
    public async Task<IActionResult> OrderDetails(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orders.GetForUserAsync(RequireUserId(), id, cancellationToken);
        if (order is null)
            return NotFound();
        ViewData["Title"] = $"سفارش {order.OrderNumber}";
        return View(order);
    }

    [HttpPost("orders/{id:guid}/cancel")]
    [Authorize(Policy = CustomerAuthDefaults.PolicyName)]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _orders.CancelForUserAsync(RequireUserId(), id, cancellationToken);
            TempData["Success"] = "سفارش لغو شد و موجودی محصولات آزاد گردید.";
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(OrderDetails), new { id });
    }

    [HttpGet("addresses")]
    [Authorize(Policy = CustomerAuthDefaults.PolicyName)]
    public async Task<IActionResult> Addresses(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "آدرس‌ها";
        return View(await _addresses.ListAsync(RequireUserId(), cancellationToken));
    }

    [HttpPost("addresses")]
    [Authorize(Policy = CustomerAuthDefaults.PolicyName)]
    public async Task<IActionResult> CreateAddress(CustomerAddressFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "اطلاعات آدرس نامعتبر است.";
            return RedirectToAction(nameof(Addresses));
        }

        if (!await _locations.IsValidLocationAsync(model.Province, model.City, cancellationToken))
        {
            TempData["Error"] = "استان یا شهر انتخاب‌شده معتبر نیست.";
            return RedirectToAction(nameof(Addresses));
        }

        try
        {
            await _addresses.CreateAsync(RequireUserId(), ToAddressCommand(model), cancellationToken);
            TempData["Success"] = "آدرس افزوده شد.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("addresses/{id:guid}/delete")]
    [Authorize(Policy = CustomerAuthDefaults.PolicyName)]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _addresses.DeleteAsync(RequireUserId(), id, cancellationToken);
            TempData["Success"] = "آدرس حذف شد.";
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("addresses/quick")]
    [Authorize(Policy = CustomerAuthDefaults.PolicyName)]
    public async Task<IActionResult> QuickCreateAddress([FromForm] CustomerAddressFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "اطلاعات آدرس نامعتبر است." });

        if (!await _locations.IsValidLocationAsync(model.Province, model.City, cancellationToken))
            return BadRequest(new { error = "استان یا شهر انتخاب‌شده معتبر نیست." });

        try
        {
            var id = await _addresses.CreateAsync(RequireUserId(), ToAddressCommand(model), cancellationToken);
            var created = await _addresses.GetAsync(RequireUserId(), id, cancellationToken);
            return Json(created);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private string RequireUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("کاربر وارد نشده است.");

    private static SaveCustomerAddressCommand ToAddressCommand(CustomerAddressFormViewModel model) =>
        new(model.RecipientName, model.RecipientPhone, model.Province, model.City, model.PostalCode, model.AddressLine, model.IsDefault);
}
