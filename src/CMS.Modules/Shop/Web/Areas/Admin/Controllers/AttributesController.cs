using CMS.Application.Common.Features;
using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Catalog;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Web;
using CMS.Modules.Shop.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Shop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewShop")]
public class AttributesController : Controller
{
    private readonly IAttributeService _attributes;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public AttributesController(IAttributeService attributes, IFeatureManager features, IStringLocalizer<ShopAdmin> localizer)
    {
        _attributes = attributes;
        _features = features;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["Attributes"].Value;
        var paged = PageSlice.FromList(await _attributes.ListAsync(cancellationToken), page);
        PageSlice.ApplyToViewBag(ViewBag, paged);
        return View(paged.Items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CreateAttribute"].Value;
        return View(new AttributeFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(AttributeFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CreateAttribute"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _attributes.CreateAsync(ToCommand(model), cancellationToken);
            TempData["Success"] = _localizer["AttributeCreated"].Value;
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

        var attr = await _attributes.GetAsync(id, cancellationToken);
        if (attr is null)
            return NotFound();

        ViewData["Title"] = _localizer["EditAttribute"].Value;
        return View(new AttributeFormViewModel
        {
            Id = attr.Id,
            Name = attr.Name,
            Slug = attr.Slug,
            SortOrder = attr.SortOrder,
            Values = attr.Values.Select(v => new AttributeValueFormViewModel
            {
                Id = v.Id,
                Value = v.Value,
                Slug = v.Slug,
                SortOrder = v.SortOrder
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, AttributeFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        model.Id = id;
        ViewData["Title"] = _localizer["EditAttribute"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _attributes.UpdateAsync(id, ToCommand(model), cancellationToken);
            TempData["Success"] = _localizer["AttributeUpdated"].Value;
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
            await _attributes.DeleteAsync(id, cancellationToken);
            TempData["Success"] = _localizer["AttributeDeleted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    private static SaveAttributeCommand ToCommand(AttributeFormViewModel model) =>
        new(
            model.Name,
            model.Slug,
            model.SortOrder,
            model.Values.Select(v => new SaveAttributeValueCommand(v.Id, v.Value, v.Slug, v.SortOrder)).ToList());

    private void AddErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
    }
}
