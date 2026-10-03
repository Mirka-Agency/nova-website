using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Products;
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
public class ProductVariationsController : Controller
{
    private readonly IProductService _products;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public ProductVariationsController(
        IProductService products,
        IFeatureManager features,
        IStringLocalizer<ShopAdmin> localizer)
    {
        _products = products;
        _features = features;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid productId, Guid? editId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var form = new ProductVariationFormViewModel();
        if (editId.HasValue)
        {
            var product = await _products.GetAsync(productId, cancellationToken);
            var variation = product?.Variations.FirstOrDefault(v => v.Id == editId.Value);
            if (product is null || variation is null)
                return NotFound();

            form = ToForm(variation);
        }

        var page = await BuildPageAsync(productId, form, cancellationToken);
        if (page is null)
            return NotFound();

        ViewData["Title"] = _localizer["ProductVariations"].Value;
        return View(page);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid productId, ProductVariationsPageViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var form = model.Form ?? new ProductVariationFormViewModel();
        form.Id = null;
        ViewData["Title"] = _localizer["ProductVariations"].Value;
        if (!ModelState.IsValid)
        {
            var invalid = await BuildPageAsync(productId, form, cancellationToken);
            return invalid is null ? NotFound() : View("Index", invalid);
        }

        try
        {
            await _products.AddVariationAsync(productId, ToCommand(form), cancellationToken);
            TempData["Success"] = _localizer["VariationCreated"].Value;
            return RedirectToAction(nameof(Index), new { productId });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var page = await BuildPageAsync(productId, form, cancellationToken);
            return page is null ? NotFound() : View("Index", page);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid productId, ProductVariationsPageViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var form = model.Form ?? new ProductVariationFormViewModel();
        ViewData["Title"] = _localizer["ProductVariations"].Value;
        if (!form.Id.HasValue)
            return RedirectToAction(nameof(Index), new { productId });

        if (!ModelState.IsValid)
        {
            var invalid = await BuildPageAsync(productId, form, cancellationToken);
            return invalid is null ? NotFound() : View("Index", invalid);
        }

        try
        {
            await _products.UpdateVariationAsync(productId, form.Id.Value, ToCommand(form), cancellationToken);
            TempData["Success"] = _localizer["VariationUpdated"].Value;
            return RedirectToAction(nameof(Index), new { productId });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var page = await BuildPageAsync(productId, form, cancellationToken);
            return page is null ? NotFound() : View("Index", page);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid productId, Guid variationId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        try
        {
            await _products.DeleteVariationAsync(productId, variationId, cancellationToken);
            TempData["Success"] = _localizer["VariationDeleted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index), new { productId });
    }

    private async Task<ProductVariationsPageViewModel?> BuildPageAsync(
        Guid productId,
        ProductVariationFormViewModel form,
        CancellationToken cancellationToken)
    {
        var product = await _products.GetAsync(productId, cancellationToken);
        if (product is null)
            return null;

        form.StatusOptions = Enum.GetValues<VariationStatus>()
            .Select(s => new SelectListItem(VariationStatusLabel(s), s.ToString(), s == form.Status));

        return new ProductVariationsPageViewModel
        {
            ProductId = product.Id,
            ProductTitle = product.Title,
            Variations = product.Variations.OrderBy(v => v.Sku).ToList(),
            Form = form
        };
    }

    private static ProductVariationFormViewModel ToForm(ProductVariationDto variation) =>
        new()
        {
            Id = variation.Id,
            Sku = variation.Sku,
            AttributeSummary = variation.AttributeSummary,
            Price = variation.Price,
            SalePrice = variation.SalePrice,
            StockQuantity = variation.StockQuantity,
            UnlimitedStock = variation.UnlimitedStock,
            ImageUrl = variation.ImageUrl,
            Weight = variation.Weight,
            Status = variation.Status
        };

    private static SaveProductVariationCommand ToCommand(ProductVariationFormViewModel form) =>
        new(
            form.Id,
            form.Sku,
            form.AttributeSummary,
            form.Price,
            form.SalePrice,
            form.StockQuantity,
            form.UnlimitedStock,
            form.ImageUrl,
            form.Weight,
            form.Status);

    private string VariationStatusLabel(VariationStatus status) => status switch
    {
        VariationStatus.Active => _localizer["StatusActive"].Value,
        VariationStatus.Hidden => _localizer["StatusHidden"].Value,
        VariationStatus.OutOfStock => _localizer["StatusOutOfStock"].Value,
        _ => status.ToString()
    };
}
