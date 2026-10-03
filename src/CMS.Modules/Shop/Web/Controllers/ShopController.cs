using CMS.Application.Admin;
using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Reviews;
using CMS.Modules.Shop.Web;
using CMS.Modules.Shop.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;
using System.Security.Claims;

namespace CMS.Modules.Shop.Web.Controllers;

[Route("shop")]
public class ShopController : Controller
{
    private readonly IPublicProductQuery _products;
    private readonly IReviewService _reviews;
    private readonly IShopSettingsService _settings;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopPublic> _localizer;

    public ShopController(
        IPublicProductQuery products,
        IReviewService reviews,
        IShopSettingsService settings,
        IFeatureManager features,
        IStringLocalizer<ShopPublic> localizer)
    {
        _products = products;
        _reviews = reviews;
        _settings = settings;
        _features = features;
        _localizer = localizer;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["Shop"].Value;
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Manage("Products", "مدیریت محصولات", "ViewShop");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _products.ListPublishedPagedAsync(page, 24, userId, null, cancellationToken);
        ViewBag.Page = result.Page;
        ViewBag.PageSize = result.PageSize;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.TotalPages = result.TotalPages;
        return View(result.Items);
    }

    [HttpGet("category/{slug}")]
    public async Task<IActionResult> Category(string slug, int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var category = await _products.GetPublishedCategoryBySlugAsync(slug, cancellationToken);
        if (category is null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var products = await _products.ListPublishedPagedAsync(page, 24, userId, category.Id, cancellationToken);

        ViewData["Title"] = category.Name;
        ViewData["MetaTitle"] = category.MetaTitle;
        ViewData["MetaDescription"] = category.MetaDescription;
        ViewData["MetaKeywords"] = category.SeoKeywords;
        ViewData["OgImage"] = category.ImageUrl;
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Edit("ShopCategories", category.Id, "ویرایش دسته‌بندی", "ManageShop");
        ViewBag.Category = category;
        ViewBag.Page = products.Page;
        ViewBag.PageSize = products.PageSize;
        ViewBag.TotalCount = products.TotalCount;
        ViewBag.TotalPages = products.TotalPages;
        return View(products.Items);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var product = await _products.GetPublishedBySlugAsync(slug, userId, cancellationToken);
        if (product is null)
            return NotFound();

        ViewData["Title"] = product.Title;
        ViewData["MetaTitle"] = product.MetaTitle;
        ViewData["MetaDescription"] = product.MetaDescription;
        ViewData["MetaKeywords"] = product.SeoKeywords;
        ViewData["CanonicalUrl"] = product.CanonicalUrl;
        ViewData["OgTitle"] = product.OgTitle;
        ViewData["OgDescription"] = product.OgDescription;
        ViewData["OgImage"] = FirstNonEmpty(product.OgImageUrl, product.CoverImageUrl);
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.Edit("Products", product.Id, "ویرایش محصول", "ManageShop");
        var settings = await _settings.GetAsync(cancellationToken);
        ViewBag.EnableReviews = settings.EnableReviews;
        return View(product);
    }

    [HttpPost("{slug}/review")]
    public async Task<IActionResult> SubmitReview(string slug, SubmitReviewFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var settings = await _settings.GetAsync(cancellationToken);
        if (!settings.EnableReviews)
            return NotFound();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = _localizer["ReviewInvalid"].Value;
            return RedirectToAction(nameof(Details), new { slug });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _reviews.SubmitAsync(new SubmitReviewCommand(
                model.ProductId, userId, model.AuthorName, model.Rating, model.Comment), cancellationToken);
            TempData["Success"] = _localizer["ReviewSubmitted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { slug });
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }
}
