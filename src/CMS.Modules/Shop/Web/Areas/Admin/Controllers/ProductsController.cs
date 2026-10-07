using CMS.Application.Common.Features;
using CMS.Application.Editing;
using CMS.Application.Storage;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Products;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Web.Areas.Admin.ViewModels;
using CMS.Modules.Shop.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using System.Globalization;

namespace CMS.Modules.Shop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewShop")]
public class ProductsController : Controller
{
    private readonly IProductService _products;
    private readonly IProductImportService _productImport;
    private readonly IShopCategoryService _categories;
    private readonly IBrandService _brands;
    private readonly IShopSettingsService _shopSettings;
    private readonly IObjectStorage _objectStorage;
    private readonly IFeatureManager _features;
    private readonly IAdminEditLockAccessor _editLocks;
    private readonly IStringLocalizer<ShopAdmin> _localizer;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService products,
        IProductImportService productImport,
        IShopCategoryService categories,
        IBrandService brands,
        IShopSettingsService shopSettings,
        IObjectStorage objectStorage,
        IFeatureManager features,
        IAdminEditLockAccessor editLocks,
        IStringLocalizer<ShopAdmin> localizer,
        ILogger<ProductsController> logger)
    {
        _products = products;
        _productImport = productImport;
        _categories = categories;
        _brands = brands;
        _shopSettings = shopSettings;
        _objectStorage = objectStorage;
        _features = features;
        _editLocks = editLocks;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IActionResult> Index(
        int page = 1,
        string? q = null,
        ProductStatus? status = null,
        Guid? categoryId = null,
        Guid? brandId = null,
        ProductType? productType = null,
        bool? isAvailable = null,
        bool? isPurchasable = null,
        string? stock = null,
        string? sort = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["Products"].Value;
        var settings = await _shopSettings.GetAsync(cancellationToken);
        var showPurchasable = !settings.IsCatalogOnly;

        var result = await _products.ListPagedAsync(
            new ProductListRequest
            {
                Page = page,
                Search = q,
                Status = status,
                CategoryId = categoryId,
                BrandId = brandId,
                ProductType = productType,
                IsAvailable = isAvailable,
                IsPurchasable = showPurchasable ? isPurchasable : null,
                Stock = stock,
                Sort = sort
            },
            cancellationToken);

        var categories = await _categories.ListAsync(cancellationToken);
        var brands = await _brands.ListAsync(cancellationToken);
        var fa = CultureInfo.GetCultureInfo("fa-IR");

        var model = new ProductIndexViewModel
        {
            Search = q,
            Status = status,
            CategoryId = categoryId,
            BrandId = brandId,
            ProductType = productType,
            IsAvailable = isAvailable,
            IsPurchasable = showPurchasable ? isPurchasable : null,
            Stock = stock,
            Sort = sort,
            Page = result.Page,
            TotalPages = result.TotalPages,
            TotalCount = result.TotalCount,
            ShowPurchasable = showPurchasable,
            CategoryOptions = categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem(c.Name, c.Id.ToString("D"), c.Id == categoryId)),
            BrandOptions = brands
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem(b.Name, b.Id.ToString("D"), b.Id == brandId)),
            Items = result.Items.Select(p => new ProductListItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                PriceDisplay = $"{p.Price.ToString("N0", fa)} {CurrencyLabel(p.Currency)}",
                Status = StatusLabel(p.Status),
                IsAvailable = p.IsAvailable,
                IsPurchasable = p.IsPurchasable,
                CategoryName = p.CategoryName,
                CoverImageUrl = p.CoverImageUrl,
                VariationCount = p.VariationCount,
                SpecificationCount = p.SpecificationCount,
                StockDisplay = FormatStockDisplay(p, fa)
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CreateProduct"].Value;
        return View(await BuildFormAsync(new ProductFormViewModel(), cancellationToken));
    }

    [HttpPost]
    [RequestSizeLimit(ImageUploadRules.MaxBytes + 1_048_576)]
    public async Task<IActionResult> Create(ProductFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        model = await BuildFormAsync(model, cancellationToken);
        ViewData["Title"] = _localizer["CreateProduct"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var cover = await ResolveCoverAsync(model, cancellationToken);
            var id = await _products.CreateAsync(ToCommand(model, cover), cancellationToken);
            _logger.LogInformation("Admin action: created product {ProductId}", id);
            TempData["Success"] = _localizer["ProductCreated"].Value;
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
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.CoverImage), ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var product = await _products.GetAsync(id, cancellationToken);
        if (product is null)
            return NotFound();

        var lockResult = await _editLocks.TryAcquireForCurrentUserAsync(
            EditLockEntityTypes.Product, id, cancellationToken);
        if (lockResult is null)
            return Challenge();
        EditLockViewBag.Apply(ViewBag, lockResult, EditLockEntityTypes.Product, id);

        ViewData["Title"] = _localizer["EditProduct"].Value;
        return View(await BuildFormAsync(new ProductFormViewModel
        {
            Id = product.Id,
            Title = product.Title,
            Slug = product.Slug,
            Sku = product.Sku,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            Price = product.Price,
            SalePrice = product.SalePrice,
            Currency = product.Currency,
            IsAvailable = product.IsAvailable,
            IsPurchasable = product.IsPurchasable,
            Status = product.Status,
            CategoryId = product.CategoryId,
            BrandId = product.BrandId,
            CoverImageUrl = product.CoverImageUrl,
            CoverImageAlt = product.CoverImageAlt,
            VideoUrl = product.VideoUrl,
            StockQuantity = product.StockQuantity,
            UnlimitedStock = product.UnlimitedStock,
            Weight = product.Weight,
            WholesaleMinimumOrderQuantity = product.WholesaleMinimumOrderQuantity,
            WholesaleMinimumOrderAmount = product.WholesaleMinimumOrderAmount,
            MetaTitle = product.MetaTitle,
            MetaDescription = product.MetaDescription,
            SeoKeywords = product.SeoKeywords,
            Images = product.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => new ProductImageFormViewModel
                {
                    Url = i.Url,
                    AltText = i.AltText,
                    SortOrder = i.SortOrder,
                    IsMain = i.IsMain
                })
                .ToList()
        }, cancellationToken));
    }

    [HttpPost]
    [RequestSizeLimit(ImageUploadRules.MaxBytes + 1_048_576)]
    public async Task<IActionResult> Edit(Guid id, ProductFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        model.Id = id;
        model = await BuildFormAsync(model, cancellationToken);
        ViewData["Title"] = _localizer["EditProduct"].Value;
        if (!ModelState.IsValid)
            return View(model);

        if (!await EnsureEditLockAsync(id, cancellationToken))
        {
            TempData["Error"] = _localizer["EditLockLost"].Value;
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var cover = await ResolveCoverAsync(model, cancellationToken);
            await _products.UpdateAsync(id, ToCommand(model, cover), cancellationToken);
            TempData["Success"] = _localizer["ProductUpdated"].Value;
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
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.CoverImage), ex.Message);
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
            await _products.DeleteAsync(id, cancellationToken);
            TempData["Success"] = _localizer["ProductDeleted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Import(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["ImportProducts"].Value;
        return View(new ProductImportViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> SampleCsv(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        return File(_productImport.GetSampleCsv(), "text/csv", "products-sample.csv");
    }

    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Import(ProductImportViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["ImportProducts"].Value;

        if (model.File is null || model.File.Length == 0)
        {
            ModelState.AddModelError(nameof(model.File), _localizer["ImportFileRequired"].Value);
            return View(model);
        }

        if (!model.File.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.File), _localizer["ImportFileMustBeCsv"].Value);
            return View(model);
        }

        try
        {
            await using var stream = model.File.OpenReadStream();
            var result = await _productImport.ImportAsync(stream, cancellationToken);
            model.Result = new ProductImportResultViewModel
            {
                TotalRows = result.TotalRows,
                CreatedCount = result.CreatedCount,
                FailedCount = result.FailedCount,
                Errors = result.Errors
                    .Select(e => new ProductImportRowErrorViewModel { RowNumber = e.RowNumber, Message = e.Message })
                    .ToList()
            };

            if (result.CreatedCount > 0)
            {
                TempData["Success"] = string.Format(
                    CultureInfo.GetCultureInfo("fa-IR"),
                    _localizer["ImportProductsSuccess"].Value,
                    result.CreatedCount,
                    result.TotalRows);
            }

            if (result.FailedCount > 0 && result.CreatedCount == 0)
                ModelState.AddModelError(string.Empty, _localizer["ImportProductsFailed"].Value);

            _logger.LogInformation(
                "Admin action: imported products. Created={CreatedCount}, Failed={FailedCount}, Total={TotalRows}",
                result.CreatedCount,
                result.FailedCount,
                result.TotalRows);

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product CSV import failed.");
            ModelState.AddModelError(string.Empty, _localizer["ImportProductsError"].Value);
            return View(model);
        }
    }

    private async Task<ProductFormViewModel> BuildFormAsync(ProductFormViewModel model, CancellationToken cancellationToken)
    {
        var categories = await _categories.ListAsync(cancellationToken);
        var brands = await _brands.ListAsync(cancellationToken);
        var settings = await _shopSettings.GetAsync(cancellationToken);

        model.ShowPurchasableOption = !settings.IsCatalogOnly;
        if (!model.ShowPurchasableOption)
            model.IsPurchasable = false;

        model.CategoryOptions =
        [
            new SelectListItem(_localizer["NoCategory"].Value, ""),
            .. categories.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == model.CategoryId))
        ];
        model.BrandOptions =
        [
            new SelectListItem(_localizer["NoBrand"].Value, ""),
            .. brands.Select(b => new SelectListItem(b.Name, b.Id.ToString(), b.Id == model.BrandId))
        ];
        model.StatusOptions = Enum.GetValues<ProductStatus>()
            .Select(s => new SelectListItem(StatusLabel(s), s.ToString(), s == model.Status));
        model.CurrencyOptions =
        [
            new SelectListItem(_localizer["CurrencyRial"].Value, "IRR", IsCurrency(model.Currency, "IRR")),
            new SelectListItem(_localizer["CurrencyToman"].Value, "TOMAN", IsCurrency(model.Currency, "TOMAN", "IRT", "TMN"))
        ];
        if (string.IsNullOrWhiteSpace(model.Currency) ||
            !model.CurrencyOptions.Any(o => string.Equals(o.Value, model.Currency, StringComparison.OrdinalIgnoreCase)))
        {
            model.Currency = "IRR";
        }

        return model;
    }

    private static bool IsCurrency(string? current, params string[] codes) =>
        codes.Any(c => string.Equals(current, c, StringComparison.OrdinalIgnoreCase));

    private string CurrencyLabel(string? currency) =>
        string.Equals(currency, "TOMAN", StringComparison.OrdinalIgnoreCase)
        || string.Equals(currency, "IRT", StringComparison.OrdinalIgnoreCase)
        || string.Equals(currency, "TMN", StringComparison.OrdinalIgnoreCase)
            ? _localizer["CurrencyToman"].Value
            : _localizer["CurrencyRial"].Value;

    private async Task<string?> ResolveCoverAsync(ProductFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.CoverImage is null || model.CoverImage.Length == 0)
            return string.IsNullOrWhiteSpace(model.CoverImageUrl) ? null : model.CoverImageUrl.Trim();

        await using var buffer = new MemoryStream();
        await model.CoverImage.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        if (!ImageUploadRules.Validate(buffer, model.CoverImage.ContentType, buffer.Length))
            throw new InvalidOperationException(_localizer["InvalidImageType"].Value);

        buffer.Position = 0;
        var key = ObjectStorageKeys.Create(ObjectStorageKeys.Modules.Shop, "products", model.CoverImage.FileName);
        var upload = await _objectStorage.UploadAsync(buffer, key, model.CoverImage.ContentType, cancellationToken);
        return upload.PublicUrl;
    }

    private static SaveProductCommand ToCommand(ProductFormViewModel model, string? coverUrl)
    {
        var images = (model.Images ?? [])
            .Where(i => !string.IsNullOrWhiteSpace(i.Url))
            .Select((i, index) => new SaveProductImageCommand(
                i.Url.Trim(),
                string.IsNullOrWhiteSpace(i.AltText) ? null : i.AltText.Trim(),
                index,
                i.IsMain))
            .ToList();

        if (images.Count > 0)
        {
            var mainIndex = images.FindIndex(i => i.IsMain);
            if (mainIndex < 0)
                mainIndex = 0;

            images = images
                .Select((image, index) => image with { IsMain = index == mainIndex, SortOrder = index })
                .ToList();

            if (string.IsNullOrWhiteSpace(coverUrl))
                coverUrl = images[mainIndex].Url;
        }

        return new(
            model.Title,
            model.Slug,
            model.ShortDescription,
            model.Description,
            model.Price,
            model.SalePrice,
            null,
            null,
            model.Currency,
            model.IsAvailable,
            model.ShowPurchasableOption && model.IsPurchasable,
            model.Status,
            model.CategoryId,
            model.BrandId,
            coverUrl,
            string.IsNullOrWhiteSpace(model.CoverImageAlt) ? null : model.CoverImageAlt.Trim(),
            model.VideoUrl,
            model.StockQuantity,
            model.UnlimitedStock,
            5,
            model.Weight,
            1,
            model.WholesaleMinimumOrderQuantity,
            model.WholesaleMinimumOrderAmount,
            string.IsNullOrWhiteSpace(model.Sku) ? null : model.Sku.Trim(),
            model.MetaTitle,
            model.MetaDescription,
            model.SeoKeywords,
            null,
            null,
            null,
            null,
            null,
            images,
            null);
    }

    private string StatusLabel(ProductStatus status) => status switch
    {
        ProductStatus.Active => _localizer["StatusActive"].Value,
        ProductStatus.Draft => _localizer["Draft"].Value,
        ProductStatus.Hidden => _localizer["StatusHidden"].Value,
        ProductStatus.OutOfStock => _localizer["StatusOutOfStock"].Value,
        _ => status.ToString()
    };

    private string FormatStockDisplay(CMS.Modules.Shop.Application.Products.ProductListItemDto product, CultureInfo culture)
    {
        if (product.UnlimitedStock)
            return _localizer["UnlimitedStock"].Value;

        if (product.VariationCount > 0)
        {
            if (product.HasUnlimitedVariation)
                return _localizer["UnlimitedStock"].Value;

            return product.VariationStockTotal.ToString("N0", culture);
        }

        return product.StockQuantity?.ToString("N0", culture) ?? "—";
    }

    private void AddErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
    }

    private async Task<bool> EnsureEditLockAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await _editLocks.CurrentUserHoldsAsync(EditLockEntityTypes.Product, id, cancellationToken: cancellationToken))
            return true;

        var acquired = await _editLocks.TryAcquireForCurrentUserAsync(
            EditLockEntityTypes.Product, id, cancellationToken);
        return acquired?.Acquired == true;
    }
}
