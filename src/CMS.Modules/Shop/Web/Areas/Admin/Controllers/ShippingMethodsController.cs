using CMS.Application.Common.Features;
using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Shipping;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Web;
using CMS.Modules.Shop.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Shop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewShop")]
public class ShippingMethodsController : Controller
{
    private readonly IShippingService _shipping;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public ShippingMethodsController(IShippingService shipping, IFeatureManager features, IStringLocalizer<ShopAdmin> localizer)
    {
        _shipping = shipping;
        _features = features;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["ShippingMethods"].Value;
        var paged = PageSlice.FromList(await _shipping.ListAsync(cancellationToken), page);
        PageSlice.ApplyToViewBag(ViewBag, paged);
        return View(paged.Items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CreateShippingMethod"].Value;
        return View(BuildForm(new ShippingMethodFormViewModel()));
    }

    [HttpPost]
    public async Task<IActionResult> Create(ShippingMethodFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CreateShippingMethod"].Value;
        if (!ModelState.IsValid)
            return View(BuildForm(model));

        try
        {
            await _shipping.CreateAsync(ToCommand(model), cancellationToken);
            TempData["Success"] = _localizer["ShippingMethodCreated"].Value;
            return RedirectToAction(nameof(Index));
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

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var method = await GetMethodAsync(id, cancellationToken);
        if (method is null)
            return NotFound();

        ViewData["Title"] = _localizer["EditShippingMethod"].Value;
        return View(BuildForm(MapToForm(method)));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, ShippingMethodFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        model.Id = id;
        ViewData["Title"] = _localizer["EditShippingMethod"].Value;
        if (!ModelState.IsValid)
            return View(BuildForm(model));

        try
        {
            await _shipping.UpdateAsync(id, ToCommand(model), cancellationToken);
            TempData["Success"] = _localizer["ShippingMethodUpdated"].Value;
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

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        try
        {
            await _shipping.DeleteAsync(id, cancellationToken);
            TempData["Success"] = _localizer["ShippingMethodDeleted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<ShippingMethodDto?> GetMethodAsync(Guid id, CancellationToken cancellationToken)
    {
        var methods = await _shipping.ListAsync(cancellationToken);
        return methods.FirstOrDefault(m => m.Id == id);
    }

    private ShippingMethodFormViewModel BuildForm(ShippingMethodFormViewModel model)
    {
        model.CalculationTypeOptions = Enum.GetValues<ShippingCalculationType>()
            .Select(t => new SelectListItem(CalculationTypeLabel(t), t.ToString(), t == model.CalculationType))
            .ToList();
        return model;
    }

    private static ShippingMethodFormViewModel MapToForm(ShippingMethodDto method) =>
        new()
        {
            Id = method.Id,
            Name = method.Name,
            Description = method.Description,
            CalculationType = method.CalculationType,
            FixedCost = method.FixedCost,
            FreeShippingMinAmount = method.FreeShippingMinAmount,
            EstimatedDeliveryText = method.EstimatedDeliveryText,
            IsActive = method.IsActive,
            SortOrder = method.SortOrder,
            Rates = method.Rates.Select(r => new ShippingRateFormViewModel
            {
                Province = r.Province,
                City = r.City,
                MinWeight = r.MinWeight,
                MaxWeight = r.MaxWeight,
                Cost = r.Cost
            }).ToList()
        };

    private static SaveShippingMethodCommand ToCommand(ShippingMethodFormViewModel model) =>
        new(
            model.Name,
            model.Description,
            model.CalculationType,
            model.FixedCost,
            model.FreeShippingMinAmount,
            model.EstimatedDeliveryText,
            model.IsActive,
            model.SortOrder,
            model.Rates?.Select(r => new SaveShippingRateCommand(r.Province, r.City, r.MinWeight, r.MaxWeight, r.Cost)).ToList());

    private string CalculationTypeLabel(ShippingCalculationType type) => type switch
    {
        ShippingCalculationType.Fixed => _localizer["ShippingCalculationFixed"].Value,
        ShippingCalculationType.WeightBased => _localizer["ShippingCalculationWeightBased"].Value,
        ShippingCalculationType.CityBased => _localizer["ShippingCalculationCityBased"].Value,
        _ => type.ToString()
    };

    private void AddErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
    }
}
