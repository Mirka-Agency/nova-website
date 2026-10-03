using CMS.Modules.Shop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Shop.Infrastructure.Persistence;

public class ShopDbContext : DbContext
{
    public ShopDbContext(DbContextOptions<ShopDbContext> options)
        : base(options)
    {
    }

    public DbSet<ShopCategory> Categories => Set<ShopCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductBrand> Brands => Set<ProductBrand>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();
    public DbSet<ProductAttribute> Attributes => Set<ProductAttribute>();
    public DbSet<ProductAttributeValue> AttributeValues => Set<ProductAttributeValue>();
    public DbSet<ProductVariation> Variations => Set<ProductVariation>();
    public DbSet<ProductVariationAttributeValue> VariationAttributeValues => Set<ProductVariationAttributeValue>();
    public DbSet<CustomerGroup> CustomerGroups => Set<CustomerGroup>();
    public DbSet<CustomerGroupMembership> CustomerGroupMemberships => Set<CustomerGroupMembership>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<WholesaleRequest> WholesaleRequests => Set<WholesaleRequest>();
    public DbSet<ShoppingCart> Carts => Set<ShoppingCart>();
    public DbSet<ShoppingCartItem> CartItems => Set<ShoppingCartItem>();
    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();
    public DbSet<ShippingRate> ShippingRates => Set<ShippingRate>();
    public DbSet<IranProvince> IranProvinces => Set<IranProvince>();
    public DbSet<IranCity> IranCities => Set<IranCity>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();
    public DbSet<ProductReview> Reviews => Set<ProductReview>();
    public DbSet<ShopSettings> Settings => Set<ShopSettings>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<PaymentProviderConfig> PaymentProviderConfigs => Set<PaymentProviderConfig>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("shop");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShopDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
