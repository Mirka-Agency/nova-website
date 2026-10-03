using CMS.Application.Common.Features;
using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Pricing;
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
public class PriceRulesController : Controller
{
    private readonly IPriceRuleService _rules;
    private readonly ICustomerGroupService _groups;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public PriceRulesController(
        IPriceRuleService rules,
        ICustomerGroupService groups,
        IFeatureManager features,
        IStringLocalizer<ShopAdmin> localizer)
    {
        _rules = rules;
        _groups = groups;
        _features = features;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["PriceRules"].Value;
        var paged = PageSlice.FromList(await _rules.ListAsync(cancellationToken), page);
        PageSlice.ApplyToViewBag(ViewBag, paged);
        return View(paged.Items);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CreatePriceRule"].Value;
        return View(await BuildFormAsync(new PriceRuleFormViewModel(), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create(PriceRuleFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        model = await BuildFormAsync(model, cancellationToken);
        ViewData["Title"] = _localizer["CreatePriceRule"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _rules.CreateAsync(ToCommand(model), cancellationToken);
            TempData["Success"] = _localizer["PriceRuleCreated"].Value;
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

        var rules = await _rules.ListAsync(cancellationToken);
        var rule = rules.FirstOrDefault(r => r.Id == id);
        if (rule is null)
            return NotFound();

        ViewData["Title"] = _localizer["EditPriceRule"].Value;
        return View(await BuildFormAsync(new PriceRuleFormViewModel
        {
            Id = rule.Id,
            Name = rule.Name,
            RuleType = rule.RuleType,
            ProductId = rule.ProductId,
            CustomerGroupId = rule.CustomerGroupId,
            MinQuantity = rule.MinQuantity,
            MaxQuantity = rule.MaxQuantity,
            FixedPrice = rule.FixedPrice,
            DiscountPercent = rule.DiscountPercent,
            IsActive = rule.IsActive,
            Priority = rule.Priority
        }, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, PriceRuleFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        model.Id = id;
        model = await BuildFormAsync(model, cancellationToken);
        ViewData["Title"] = _localizer["EditPriceRule"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _rules.UpdateAsync(id, ToCommand(model), cancellationToken);
            TempData["Success"] = _localizer["PriceRuleUpdated"].Value;
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
            await _rules.DeleteAsync(id, cancellationToken);
            TempData["Success"] = _localizer["PriceRuleDeleted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<PriceRuleFormViewModel> BuildFormAsync(PriceRuleFormViewModel model, CancellationToken cancellationToken)
    {
        var groups = await _groups.ListAsync(cancellationToken);
        model.GroupOptions = groups.Select(g => new SelectListItem(g.Name, g.Id.ToString(), g.Id == model.CustomerGroupId)).ToList();
        model.RuleTypeOptions = Enum.GetValues<PriceRuleType>()
            .Select(t => new SelectListItem(t.ToString(), t.ToString(), t == model.RuleType));
        return model;
    }

    private static SavePriceRuleCommand ToCommand(PriceRuleFormViewModel model) =>
        new(model.Name, model.RuleType, model.ProductId, null, model.CustomerGroupId, model.MinQuantity, model.MaxQuantity,
            model.FixedPrice, model.DiscountPercent, null, null, model.IsActive, model.Priority);

    private void AddErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
    }
}
