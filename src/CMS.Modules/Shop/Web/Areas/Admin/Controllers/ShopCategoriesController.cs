using CMS.Application.Common.Features;
using CMS.Application.Common.Paging;
using CMS.Application.Editing;
using CMS.Application.Storage;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Categories;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Web.Areas.Admin.ViewModels;
using CMS.Modules.Shop.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Shop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewShop")]
public class ShopCategoriesController : Controller
{
    private readonly IShopCategoryService _categories;
    private readonly IBrandService _brands;
    private readonly IObjectStorage _objectStorage;
    private readonly IFeatureManager _features;
    private readonly IAdminEditLockAccessor _editLocks;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public ShopCategoriesController(
        IShopCategoryService categories,
        IBrandService brands,
        IObjectStorage objectStorage,
        IFeatureManager features,
        IAdminEditLockAccessor editLocks,
        IStringLocalizer<ShopAdmin> localizer)
    {
        _categories = categories;
        _brands = brands;
        _objectStorage = objectStorage;
        _features = features;
        _editLocks = editLocks;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(string? tab, int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CategoriesAndBrands"].Value;

        var activeTab = NormalizeTab(tab);
        var isBrands = activeTab == "brands";

        var categories = await _categories.ListAsync(cancellationToken);
        var brands = await _brands.ListAsync(cancellationToken);

        var categoryItems = categories.Select(c => new ShopCategoryListItemViewModel
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            ProductCount = c.ProductCount,
            ImageUrl = c.ImageUrl,
            IsActive = c.IsActive
        }).ToList();

        IReadOnlyList<ShopCategoryListItemViewModel> pagedCategories;
        IReadOnlyList<CMS.Modules.Shop.Application.Catalog.BrandListItemDto> pagedBrands;

        if (isBrands)
        {
            var paged = PageSlice.FromList(brands, page);
            PageSlice.ApplyToViewBag(ViewBag, paged);
            pagedBrands = paged.Items;
            pagedCategories = categoryItems;
        }
        else
        {
            var paged = PageSlice.FromList(categoryItems, page);
            PageSlice.ApplyToViewBag(ViewBag, paged);
            pagedCategories = paged.Items;
            pagedBrands = brands;
        }

        return View(new CategoriesAndBrandsPageViewModel
        {
            ActiveTab = activeTab,
            Categories = pagedCategories,
            Brands = pagedBrands
        });
    }

    private static string NormalizeTab(string? tab) =>
        string.Equals(tab, "brands", StringComparison.OrdinalIgnoreCase) ? "brands" : "categories";

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CreateCategory"].Value;
        return View(new ShopCategoryFormViewModel());
    }

    [HttpPost]
    [RequestSizeLimit(ImageUploadRules.MaxBytes + 1_048_576)]
    public async Task<IActionResult> Create(ShopCategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CreateCategory"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var imageUrl = await ResolveImageAsync(model, cancellationToken);
            await _categories.CreateAsync(ToCommand(model, imageUrl), cancellationToken);
            TempData["Success"] = _localizer["CategoryCreated"].Value;
            return RedirectToAction(nameof(Index), new { tab = "categories" });
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
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.Image), ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var category = await _categories.GetAsync(id, cancellationToken);
        if (category is null)
            return NotFound();

        var lockResult = await _editLocks.TryAcquireForCurrentUserAsync(
            EditLockEntityTypes.ShopCategory, id, cancellationToken);
        if (lockResult is null)
            return Challenge();
        EditLockViewBag.Apply(ViewBag, lockResult, EditLockEntityTypes.ShopCategory, id);

        ViewData["Title"] = _localizer["EditCategory"].Value;
        return View(new ShopCategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            Content = category.Content,
            ImageUrl = category.ImageUrl,
            MetaTitle = category.MetaTitle,
            MetaDescription = category.MetaDescription,
            SeoKeywords = category.SeoKeywords,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive
        });
    }

    [HttpPost]
    [RequestSizeLimit(ImageUploadRules.MaxBytes + 1_048_576)]
    public async Task<IActionResult> Edit(Guid id, ShopCategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        model.Id = id;
        ViewData["Title"] = _localizer["EditCategory"].Value;
        if (!ModelState.IsValid)
            return View(model);

        if (!await EnsureEditLockAsync(id, cancellationToken))
        {
            TempData["Error"] = _localizer["EditLockLost"].Value;
            return RedirectToAction(nameof(Index), new { tab = "categories" });
        }

        try
        {
            var imageUrl = await ResolveImageAsync(model, cancellationToken);
            await _categories.UpdateAsync(id, ToCommand(model, imageUrl), cancellationToken);
            TempData["Success"] = _localizer["CategoryUpdated"].Value;
            return RedirectToAction(nameof(Index), new { tab = "categories" });
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
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.Image), ex.Message);
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
            await _categories.DeleteAsync(id, cancellationToken);
            TempData["Success"] = _localizer["CategoryDeleted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index), new { tab = "categories" });
    }

    private async Task<string?> ResolveImageAsync(ShopCategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.Image is null || model.Image.Length == 0)
            return string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim();

        await using var buffer = new MemoryStream();
        await model.Image.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        if (!ImageUploadRules.Validate(buffer, model.Image.ContentType, buffer.Length))
            throw new InvalidOperationException(_localizer["InvalidImageType"].Value);

        buffer.Position = 0;
        var key = ObjectStorageKeys.Create(ObjectStorageKeys.Modules.Shop, "categories", model.Image.FileName);
        var upload = await _objectStorage.UploadAsync(buffer, key, model.Image.ContentType, cancellationToken);
        return upload.PublicUrl;
    }

    private static SaveShopCategoryCommand ToCommand(ShopCategoryFormViewModel model, string? imageUrl) =>
        new(
            model.Name,
            model.Slug,
            model.Description,
            model.Content,
            imageUrl,
            model.MetaTitle,
            model.MetaDescription,
            model.SeoKeywords,
            model.SortOrder,
            model.IsActive);

    private void AddErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
    }

    private async Task<bool> EnsureEditLockAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await _editLocks.CurrentUserHoldsAsync(EditLockEntityTypes.ShopCategory, id, cancellationToken: cancellationToken))
            return true;

        var acquired = await _editLocks.TryAcquireForCurrentUserAsync(
            EditLockEntityTypes.ShopCategory, id, cancellationToken);
        return acquired?.Acquired == true;
    }
}
