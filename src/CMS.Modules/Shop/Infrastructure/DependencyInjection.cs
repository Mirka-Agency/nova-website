using CMS.Application.Seo;
using CMS.Modules.Shop.Application;
using CMS.Modules.Shop.Application.Account;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Locations;
using CMS.Modules.Shop.Infrastructure.Locations;
using CMS.Modules.Shop.Infrastructure.Persistence;
using CMS.Modules.Shop.Infrastructure.Seo;
using CMS.Modules.Shop.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Shop.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddShopModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddShopApplication();
        services.AddHttpContextAccessor();

        services.AddDbContext<ShopDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services.AddScoped<IShopCategoryService, ShopCategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductImportService, ProductImportService>();
        services.AddScoped<IPublicProductQuery, PublicProductQuery>();
        services.AddScoped<ISitemapUrlProvider, ShopSitemapUrlProvider>();
        services.AddScoped<IShopSettingsService, ShopSettingsService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IAttributeService, AttributeService>();
        services.AddScoped<ICustomerGroupService, CustomerGroupService>();
        services.AddScoped<IPriceRuleService, PriceRuleService>();
        services.AddScoped<IPricingEngine, PricingEngine>();
        services.AddScoped<IWholesaleService, WholesaleService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<IShippingService, ShippingService>();
        services.AddScoped<IIranLocationService, IranLocationService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ICommerceReportService, CommerceReportService>();
        services.AddScoped<ICustomerAddressService, CustomerAddressService>();
        services.AddScoped<IShopOrderNotifier, ShopOrderNotifier>();
        services.AddScoped<IPaymentConfigService, PaymentConfigService>();
        services.AddScoped<IPaymentOrchestrator, PaymentOrchestrator>();
        services.AddHostedService<PendingOrderMaintenanceService>();

        return services;
    }
}
