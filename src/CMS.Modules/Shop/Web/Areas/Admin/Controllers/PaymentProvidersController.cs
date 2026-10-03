using CMS.Application.Common.Features;
using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Payments;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Web;
using CMS.Modules.Shop.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Shop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewShop")]
public class PaymentProvidersController : Controller
{
    private readonly IPaymentConfigService _payments;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;
    private readonly IHostEnvironment _environment;

    public PaymentProvidersController(
        IPaymentConfigService payments,
        IFeatureManager features,
        IStringLocalizer<ShopAdmin> localizer,
        IHostEnvironment environment)
    {
        _payments = payments;
        _features = features;
        _localizer = localizer;
        _environment = environment;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["PaymentProviders"].Value;
        var paged = PageSlice.FromList(await _payments.ListAsync(cancellationToken), page);
        PageSlice.ApplyToViewBag(ViewBag, paged);
        return View(paged.Items);
    }

    [HttpGet]
    [Authorize(Policy = "ManageShop")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var provider = await _payments.GetAsync(id, cancellationToken);
        if (provider is null)
            return NotFound();

        ViewData["Title"] = _localizer["EditPaymentProvider"].Value;
        return View(BuildForm(MapToForm(provider)));
    }

    [HttpPost]
    [Authorize(Policy = "ManageShop")]
    public async Task<IActionResult> Edit(Guid id, PaymentProviderFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        model.Id = id;
        ViewData["Title"] = _localizer["EditPaymentProvider"].Value;
        if (!ModelState.IsValid)
            return View(BuildForm(model));

        try
        {
            await _payments.UpdateAsync(id, ToCommand(model), cancellationToken);
            TempData["Success"] = _localizer["PaymentProviderUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ValidationException ex)
        {
            AddErrors(ex);
            return View(BuildForm(model));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(BuildForm(model));
        }
    }

    private PaymentProviderFormViewModel BuildForm(PaymentProviderFormViewModel model)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        model.CallbackUrl = $"{baseUrl.TrimEnd('/')}/shop/payment/callback/{model.ProviderType.ToString().ToLowerInvariant()}";
        model.SandboxAllowed = !_environment.IsProduction();
        if (!model.SandboxAllowed)
            model.IsSandbox = false;
        return model;
    }

    private PaymentProviderFormViewModel MapToForm(PaymentProviderConfigDto provider) =>
        new()
        {
            Id = provider.Id,
            ProviderType = provider.ProviderType,
            DisplayName = provider.DisplayName,
            IsEnabled = provider.IsEnabled,
            IsSandbox = provider.IsSandbox && !_environment.IsProduction(),
            SandboxAllowed = !_environment.IsProduction(),
            SortOrder = provider.SortOrder,
            MerchantId = provider.Settings.MerchantId,
            TerminalId = provider.Settings.TerminalId,
            Username = provider.Settings.Username,
            // Never echo secrets to the browser; leave blank to keep existing values on save.
            Password = string.IsNullOrWhiteSpace(provider.Settings.Password) ? null : string.Empty,
            ClientId = provider.Settings.ClientId,
            ClientSecret = string.IsNullOrWhiteSpace(provider.Settings.ClientSecret) ? null : string.Empty
        };

    private SavePaymentProviderCommand ToCommand(PaymentProviderFormViewModel model) =>
        new(
            model.DisplayName,
            model.IsEnabled,
            model.IsSandbox && !_environment.IsProduction(),
            model.SortOrder,
            new PaymentProviderSettingsDto
            {
                MerchantId = model.MerchantId,
                TerminalId = model.TerminalId,
                Username = model.Username,
                Password = model.Password,
                ClientId = model.ClientId,
                ClientSecret = model.ClientSecret
            });

    private void AddErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
    }
}
