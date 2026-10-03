using CMS.Application.Security;
using CMS.Infrastructure.Identity;
using CMS.Web.Areas.Admin.ViewModels;
using CMS.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace CMS.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAdminLoginCaptchaService _loginCaptcha;
    private readonly ILogger<AccountController> _logger;
    private readonly IStringLocalizer<AdminShared> _localizer;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAdminLoginCaptchaService loginCaptcha,
        ILogger<AccountController> logger,
        IStringLocalizer<AdminShared> localizer)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _loginCaptcha = loginCaptcha;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(ApplyCaptcha(new LoginViewModel { ReturnUrl = returnUrl }));
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingConfiguration.AuthPolicy)]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        model.ReturnUrl ??= Url.Action("Index", "Dashboard", new { area = "Admin" });
        ApplyCaptcha(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var captcha = await _loginCaptcha.ValidateAsync(
            model.CaptchaToken,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        if (!captcha.Succeeded)
        {
            _logger.LogWarning("Security: admin login captcha failed for {Email}", model.Email);
            ModelState.AddModelError(nameof(model.CaptchaToken), captcha.ErrorMessage ?? _localizer["LoginCaptchaFailed"]);
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            _logger.LogWarning("Security: login failed for unknown email {Email}", model.Email);
            ModelState.AddModelError(string.Empty, _localizer["LoginFailed"]);
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("Security: user {UserId} signed in", user.Id);
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Security: user {UserId} locked out", user.Id);
            ModelState.AddModelError(string.Empty, _localizer["AccountLocked"]);
            return View(model);
        }

        _logger.LogWarning("Security: login failed for user {UserId}", user.Id);
        ModelState.AddModelError(string.Empty, _localizer["LoginFailed"]);
        return View(model);
    }

    private LoginViewModel ApplyCaptcha(LoginViewModel model)
    {
        var display = _loginCaptcha.GetDisplay();
        model.CaptchaEnabled = display.Enabled;
        model.CaptchaProvider = display.Provider;
        model.CaptchaSiteKey = display.SiteKey;
        return model;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Security: user {UserId} signed out", userId);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View(new ProfileEditViewModel
        {
            Email = user.Email ?? string.Empty,
            FullName = user.FullName ?? string.Empty,
            AvatarUrl = user.AvatarUrl,
            PhoneNumber = user.PhoneNumber
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Profile(ProfileEditViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        model.Email = user.Email ?? string.Empty;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.AvatarUrl = string.IsNullOrWhiteSpace(model.AvatarUrl) ? null : model.AvatarUrl.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber)
            ? null
            : model.PhoneNumber.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, IdentityErrorLocalizer.Localize(error));
            }

            return View(model);
        }

        _logger.LogInformation("Security: user {UserId} updated profile", user.Id);
        TempData["Success"] = _localizer["ProfileUpdated"].Value;
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting(RateLimitingConfiguration.AuthPolicy)]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, IdentityErrorLocalizer.Localize(error));
            }

            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        _logger.LogInformation("Security: user {UserId} changed password", user.Id);
        TempData["Success"] = _localizer["PasswordChanged"].Value;
        return RedirectToAction(nameof(ChangePassword));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        _logger.LogWarning("Security: access denied for {User} on {Path}",
            User.Identity?.Name ?? "anonymous", HttpContext.Request.Path);
        return View();
    }
}
