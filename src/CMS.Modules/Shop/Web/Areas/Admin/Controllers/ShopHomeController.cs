using CMS.Application.Common.Features;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Web.Areas.Admin.ViewModels;
using CMS.Modules.Shop.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Shop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewShop")]
public class ShopHomeController : Controller
{
    private readonly IProductService _products;
    private readonly IOrderService _orders;
    private readonly IShopCategoryService _categories;
    private readonly IShopSettingsService _settings;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public ShopHomeController(
        IProductService products,
        IOrderService orders,
        IShopCategoryService categories,
        IShopSettingsService settings,
        IFeatureManager features,
        IStringLocalizer<ShopAdmin> localizer)
    {
        _products = products;
        _orders = orders;
        _categories = categories;
        _settings = settings;
        _features = features;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["Shop"].Value;

        var products = await _products.ListAsync(cancellationToken);
        var orders = await _orders.ListAsync(cancellationToken);
        var categories = await _categories.ListAsync(cancellationToken);
        var settings = await _settings.GetAsync(cancellationToken);

        return View(new ShopHomeViewModel
        {
            ProductCount = products.Count,
            PublishedProductCount = products.Count(p => p.Status == ProductStatus.Active),
            CategoryCount = categories.Count,
            OrderCount = orders.Count,
            ProcessingOrderCount = orders.Count(o =>
                o.Status is OrderStatus.Pending or OrderStatus.Reviewing or OrderStatus.Processing),
            ModeDisplay = settings.Mode switch
            {
                ShopMode.CatalogOnly => _localizer["CatalogOnlyMode"].Value,
                ShopMode.OnlineStore => _localizer["OnlineStoreMode"].Value,
                ShopMode.Hybrid => _localizer["HybridMode"].Value,
                _ => settings.Mode.ToString()
            },
            RecentOrders = orders.Take(5).Select(o => new OrderListItemViewModel
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.CustomerName,
                CustomerEmail = o.CustomerEmail,
                StatusDisplay = o.Status switch
                {
                    OrderStatus.Pending => _localizer["StatusPendingPayment"].Value,
                    OrderStatus.Processing => _localizer["StatusProcessing"].Value,
                    OrderStatus.Reviewing => _localizer["StatusProcessing"].Value,
                    OrderStatus.Shipped => _localizer["StatusShipped"].Value,
                    OrderStatus.Cancelled => _localizer["StatusCancelled"].Value,
                    _ => o.Status.ToString()
                },
                TotalDisplay = $"{o.TotalAmount:N0} {o.Currency}",
                CreatedAtUtc = o.CreatedAtUtc,
                Status = o.Status,
                PaymentExpiresAtUtc = o.PaymentExpiresAtUtc
            }).ToList()
        });
    }
}
