using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Products;
using CMS.Modules.Shop.Web;
using CMS.Modules.Shop.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Shop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewShop")]
public class ProductSpecificationsController : Controller
{
    private readonly IProductService _products;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public ProductSpecificationsController(
        IProductService products,
        IFeatureManager features,
        IStringLocalizer<ShopAdmin> localizer)
    {
        _products = products;
        _features = features;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid productId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var page = await BuildPageAsync(productId, null, cancellationToken);
        if (page is null)
            return NotFound();

        ViewData["Title"] = _localizer["ProductSpecifications"].Value;
        return View(page);
    }

    [HttpPost]
    public async Task<IActionResult> Save(Guid productId, ProductSpecificationsPageViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["ProductSpecifications"].Value;
        model.ProductId = productId;
        model.Items ??= [];

        // Drop blank rows before validation.
        model.Items = model.Items
            .Where(i => !string.IsNullOrWhiteSpace(i.Key) || !string.IsNullOrWhiteSpace(i.Value))
            .ToList();

        for (var i = 0; i < model.Items.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(model.Items[i].Key))
                ModelState.AddModelError($"Items[{i}].Key", _localizer["SpecificationKeyRequired"].Value);
            if (string.IsNullOrWhiteSpace(model.Items[i].Value))
                ModelState.AddModelError($"Items[{i}].Value", _localizer["SpecificationValueRequired"].Value);
        }

        if (!ModelState.IsValid)
        {
            var invalid = await BuildPageAsync(productId, model.Items, cancellationToken);
            return invalid is null ? NotFound() : View("Index", invalid);
        }

        try
        {
            var commands = model.Items
                .Select((item, index) => new SaveProductSpecificationCommand(
                    item.Key.Trim(),
                    item.Value.Trim(),
                    index))
                .ToList();

            await _products.ReplaceSpecificationsAsync(productId, commands, cancellationToken);
            TempData["Success"] = _localizer["SpecificationsSaved"].Value;
            return RedirectToAction(nameof(Index), new { productId });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var page = await BuildPageAsync(productId, model.Items, cancellationToken);
            return page is null ? NotFound() : View("Index", page);
        }
    }

    private async Task<ProductSpecificationsPageViewModel?> BuildPageAsync(
        Guid productId,
        List<ProductSpecificationFormViewModel>? items,
        CancellationToken cancellationToken)
    {
        var product = await _products.GetAsync(productId, cancellationToken);
        if (product is null)
            return null;

        return new ProductSpecificationsPageViewModel
        {
            ProductId = product.Id,
            ProductTitle = product.Title,
            Items = items ?? product.Specifications
                .OrderBy(s => s.SortOrder)
                .Select(s => new ProductSpecificationFormViewModel
                {
                    Key = s.Key,
                    Value = s.Value,
                    SortOrder = s.SortOrder
                })
                .ToList()
        };
    }
}
