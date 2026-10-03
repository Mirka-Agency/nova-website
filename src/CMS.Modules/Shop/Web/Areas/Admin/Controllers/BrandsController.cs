using CMS.Application.Common.Features;
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
public class BrandsController : Controller
{
    private readonly IBrandService _brands;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public BrandsController(IBrandService brands, IFeatureManager features, IStringLocalizer<ShopAdmin> localizer)
    {
        _brands = brands;
        _features = features;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        return RedirectToAction("Index", "ShopCategories", new { tab = "brands" });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CreateBrand"].Value;
        return View(new BrandFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(BrandFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CreateBrand"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _brands.CreateAsync(
                new SaveBrandCommand(
                    model.Name,
                    model.Slug,
                    model.Description,
                    model.Content,
                    model.LogoUrl,
                    model.MetaTitle,
                    model.MetaDescription,
                    model.SeoKeywords,
                    model.IsActive),
                cancellationToken);
            TempData["Success"] = _localizer["BrandCreated"].Value;
            return RedirectToAction("Index", "ShopCategories", new { tab = "brands" });
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

        var brand = await _brands.GetAsync(id, cancellationToken);
        if (brand is null)
            return NotFound();

        ViewData["Title"] = _localizer["EditBrand"].Value;
        return View(new BrandFormViewModel
        {
            Id = brand.Id,
            Name = brand.Name,
            Slug = brand.Slug,
            Description = brand.Description,
            Content = brand.Content,
            LogoUrl = brand.LogoUrl,
            MetaTitle = brand.MetaTitle,
            MetaDescription = brand.MetaDescription,
            SeoKeywords = brand.SeoKeywords,
            IsActive = brand.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, BrandFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        model.Id = id;
        ViewData["Title"] = _localizer["EditBrand"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _brands.UpdateAsync(
                id,
                new SaveBrandCommand(
                    model.Name,
                    model.Slug,
                    model.Description,
                    model.Content,
                    model.LogoUrl,
                    model.MetaTitle,
                    model.MetaDescription,
                    model.SeoKeywords,
                    model.IsActive),
                cancellationToken);
            TempData["Success"] = _localizer["BrandUpdated"].Value;
            return RedirectToAction("Index", "ShopCategories", new { tab = "brands" });
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
            await _brands.DeleteAsync(id, cancellationToken);
            TempData["Success"] = _localizer["BrandDeleted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction("Index", "ShopCategories", new { tab = "brands" });
    }

    private void AddErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
    }
}
