using CMS.Application.Common.Features;
using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Pricing;
using CMS.Modules.Shop.Web;
using CMS.Modules.Shop.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Shop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewShop")]
public class CustomerGroupsController : Controller
{
    private readonly ICustomerGroupService _groups;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public CustomerGroupsController(ICustomerGroupService groups, IFeatureManager features, IStringLocalizer<ShopAdmin> localizer)
    {
        _groups = groups;
        _features = features;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CustomerGroups"].Value;
        var paged = PageSlice.FromList(await _groups.ListAsync(cancellationToken), page);
        PageSlice.ApplyToViewBag(ViewBag, paged);
        return View(paged.Items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CreateCustomerGroup"].Value;
        return View(new CustomerGroupFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomerGroupFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CreateCustomerGroup"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _groups.CreateAsync(ToCommand(model), cancellationToken);
            TempData["Success"] = _localizer["CustomerGroupCreated"].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            AddErrors(ex);
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var groups = await _groups.ListAsync(cancellationToken);
        var group = groups.FirstOrDefault(g => g.Id == id);
        if (group is null)
            return NotFound();

        ViewData["Title"] = _localizer["EditCustomerGroup"].Value;
        return View(new CustomerGroupFormViewModel
        {
            Id = group.Id,
            Name = group.Name,
            Code = group.Code,
            Description = group.Description,
            IsWholesale = group.IsWholesale,
            SortOrder = group.SortOrder,
            IsActive = group.IsActive,
            MinimumOrderAmount = group.MinimumOrderAmount,
            MinimumOrderQuantity = group.MinimumOrderQuantity
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, CustomerGroupFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        model.Id = id;
        ViewData["Title"] = _localizer["EditCustomerGroup"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _groups.UpdateAsync(id, ToCommand(model), cancellationToken);
            TempData["Success"] = _localizer["CustomerGroupUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ValidationException ex)
        {
            AddErrors(ex);
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        try
        {
            await _groups.DeleteAsync(id, cancellationToken);
            TempData["Success"] = _localizer["CustomerGroupDeleted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    private static SaveCustomerGroupCommand ToCommand(CustomerGroupFormViewModel model) =>
        new(model.Name, model.Code, model.Description, model.IsWholesale, model.SortOrder, model.IsActive,
            model.MinimumOrderAmount, model.MinimumOrderQuantity);

    private void AddErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
    }
}
