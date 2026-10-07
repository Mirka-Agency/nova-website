using CMS.Modules.Shop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Modules.Shop.Infrastructure.Persistence.Configurations;

public sealed class ShopCategoryConfiguration : IEntityTypeConfiguration<ShopCategory>
{
    public void Configure(EntityTypeBuilder<ShopCategory> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Content);
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);
        builder.Property(x => x.MetaTitle).HasMaxLength(200);
        builder.Property(x => x.MetaDescription).HasMaxLength(500);
        builder.Property(x => x.SeoKeywords).HasMaxLength(500);
        builder.HasIndex(x => x.Slug).IsUnique();

        builder.HasMany(x => x.Products)
            .WithOne(x => x.Category)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Navigation(x => x.Products).HasField("_products");
        builder.Metadata.FindNavigation(nameof(ShopCategory.Products))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ShortDescription).HasMaxLength(2000);
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Price).HasPrecision(18, 2);
        builder.Property(x => x.SalePrice).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ProductType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CoverImageUrl).HasMaxLength(1000);
        builder.Property(x => x.CoverImageAlt).HasMaxLength(300);
        builder.Property(x => x.VideoUrl).HasMaxLength(1000);
        builder.Property(x => x.Sku).HasMaxLength(100);
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.Property(x => x.Weight).HasPrecision(18, 3);
        builder.Property(x => x.WholesaleMinimumOrderAmount).HasPrecision(18, 2);
        builder.Property(x => x.MetaTitle).HasMaxLength(200);
        builder.Property(x => x.MetaDescription).HasMaxLength(500);
        builder.Property(x => x.SeoKeywords).HasMaxLength(500);
        builder.Property(x => x.CanonicalUrl).HasMaxLength(1000);
        builder.Property(x => x.OgTitle).HasMaxLength(200);
        builder.Property(x => x.OgDescription).HasMaxLength(500);
        builder.Property(x => x.OgImageUrl).HasMaxLength(1000);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.Status, x.PublishedAtUtc });

        builder.HasOne(x => x.Brand)
            .WithMany()
            .HasForeignKey(x => x.BrandId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Images).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Variations).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Specifications).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Images).HasField("_images");
        builder.Navigation(x => x.Variations).HasField("_variations");
        builder.Navigation(x => x.Specifications).HasField("_specifications");
        builder.Metadata.FindNavigation(nameof(Product.Images))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Product.Variations))!.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Product.Specifications))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ProductSpecificationConfiguration : IEntityTypeConfiguration<ProductSpecification>
{
    public void Configure(EntityTypeBuilder<ProductSpecification> builder)
    {
        builder.ToTable("ProductSpecifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.SortOrder });
    }
}

public sealed class ProductBrandConfiguration : IEntityTypeConfiguration<ProductBrand>
{
    public void Configure(EntityTypeBuilder<ProductBrand> builder)
    {
        builder.ToTable("Brands");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Content);
        builder.Property(x => x.LogoUrl).HasMaxLength(1000);
        builder.Property(x => x.MetaTitle).HasMaxLength(200);
        builder.Property(x => x.MetaDescription).HasMaxLength(500);
        builder.Property(x => x.SeoKeywords).HasMaxLength(500);
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.AltText).HasMaxLength(300);
    }
}

public sealed class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.ToTable("Attributes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasMany(x => x.Values).WithOne(x => x.Attribute).HasForeignKey(x => x.AttributeId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Values).HasField("_values");
        builder.Metadata.FindNavigation(nameof(ProductAttribute.Values))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.ToTable("AttributeValues");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Value).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
    }
}

public sealed class ProductVariationConfiguration : IEntityTypeConfiguration<ProductVariation>
{
    public void Configure(EntityTypeBuilder<ProductVariation> builder)
    {
        builder.ToTable("Variations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Sku).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AttributeSummary).HasMaxLength(500);
        builder.Property(x => x.Price).HasPrecision(18, 2);
        builder.Property(x => x.SalePrice).HasPrecision(18, 2);
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);
        builder.Property(x => x.Weight).HasPrecision(18, 3);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.Sku);
    }
}

public sealed class ProductVariationAttributeValueConfiguration : IEntityTypeConfiguration<ProductVariationAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductVariationAttributeValue> builder)
    {
        builder.ToTable("VariationAttributeValues");
        builder.HasKey(x => new { x.VariationId, x.AttributeValueId });
        builder.HasOne(x => x.Variation).WithMany().HasForeignKey(x => x.VariationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.AttributeValue).WithMany().HasForeignKey(x => x.AttributeValueId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CustomerGroupConfiguration : IEntityTypeConfiguration<CustomerGroup>
{
    public void Configure(EntityTypeBuilder<CustomerGroup> builder)
    {
        builder.ToTable("CustomerGroups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.MinimumOrderAmount).HasPrecision(18, 2);
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class CustomerGroupMembershipConfiguration : IEntityTypeConfiguration<CustomerGroupMembership>
{
    public void Configure(EntityTypeBuilder<CustomerGroupMembership> builder)
    {
        builder.ToTable("CustomerGroupMemberships");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.HasOne(x => x.CustomerGroup).WithMany().HasForeignKey(x => x.CustomerGroupId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}

public sealed class PriceRuleConfiguration : IEntityTypeConfiguration<PriceRule>
{
    public void Configure(EntityTypeBuilder<PriceRule> builder)
    {
        builder.ToTable("PriceRules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RuleType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.FixedPrice).HasPrecision(18, 2);
        builder.Property(x => x.DiscountPercent).HasPrecision(9, 2);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.CustomerGroup).WithMany().HasForeignKey(x => x.CustomerGroupId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => new { x.IsActive, x.ProductId, x.VariationId });
    }
}

public sealed class WholesaleRequestConfiguration : IEntityTypeConfiguration<WholesaleRequest>
{
    public void Configure(EntityTypeBuilder<WholesaleRequest> builder)
    {
        builder.ToTable("WholesaleRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BusinessInfo).HasMaxLength(4000);
        builder.Property(x => x.ContactName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.AdminNotes).HasMaxLength(2000);
    }
}

public sealed class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> builder)
    {
        builder.ToTable("Carts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.Property(x => x.SessionId).HasMaxLength(100);
        builder.Property(x => x.CouponCode).HasMaxLength(50);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.SessionId);
        builder.HasMany(x => x.Items).WithOne(x => x.Cart).HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).HasField("_items");
        builder.Metadata.FindNavigation(nameof(ShoppingCart.Items))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ShoppingCartItemConfiguration : IEntityTypeConfiguration<ShoppingCartItem>
{
    public void Configure(EntityTypeBuilder<ShoppingCartItem> builder)
    {
        builder.ToTable("CartItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => new { x.CartId, x.ProductId, x.VariationId });
    }
}

public sealed class ShippingMethodConfiguration : IEntityTypeConfiguration<ShippingMethod>
{
    public void Configure(EntityTypeBuilder<ShippingMethod> builder)
    {
        builder.ToTable("ShippingMethods");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.EstimatedDeliveryText).HasMaxLength(200);
        builder.Property(x => x.CalculationType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.FixedCost).HasPrecision(18, 2);
        builder.Property(x => x.FreeShippingMinAmount).HasPrecision(18, 2);
        builder.HasMany(x => x.Rates).WithOne(x => x.ShippingMethod).HasForeignKey(x => x.ShippingMethodId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Rates).HasField("_rates");
        builder.Metadata.FindNavigation(nameof(ShippingMethod.Rates))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ShippingRateConfiguration : IEntityTypeConfiguration<ShippingRate>
{
    public void Configure(EntityTypeBuilder<ShippingRate> builder)
    {
        builder.ToTable("ShippingRates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Province).HasMaxLength(100);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.MinWeight).HasPrecision(18, 3);
        builder.Property(x => x.MaxWeight).HasPrecision(18, 3);
        builder.Property(x => x.Cost).HasPrecision(18, 2);
    }
}

public sealed class IranProvinceConfiguration : IEntityTypeConfiguration<IranProvince>
{
    public void Configure(EntityTypeBuilder<IranProvince> builder)
    {
        builder.ToTable("IranProvinces");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasMany(x => x.Cities).WithOne(x => x.Province).HasForeignKey(x => x.ProvinceId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Cities).HasField("_cities");
        builder.Metadata.FindNavigation(nameof(IranProvince.Cities))!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class IranCityConfiguration : IEntityTypeConfiguration<IranCity>
{
    public void Configure(EntityTypeBuilder<IranCity> builder)
    {
        builder.ToTable("IranCities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.ProvinceId, x.Name }).IsUnique();
    }
}

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DiscountType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.MinOrderAmount).HasPrecision(18, 2);
        builder.Property(x => x.AllowedUserId).HasMaxLength(450);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.AllowedUserId);
    }
}

public sealed class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> builder)
    {
        builder.ToTable("CouponRedemptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.HasOne(x => x.Coupon).WithMany().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("Reviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.Property(x => x.AuthorName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(4000);
        builder.Property(x => x.AdminResponse).HasMaxLength(2000);
        builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => new { x.ProductId, x.IsApproved });
    }
}

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.UserId).HasMaxLength(450);
        builder.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CustomerEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CustomerPhone).HasMaxLength(40);
        builder.Property(x => x.RecipientName).HasMaxLength(200);
        builder.Property(x => x.RecipientPhone).HasMaxLength(40);
        builder.Property(x => x.ShippingAddress).HasMaxLength(1000);
        builder.Property(x => x.ShippingCity).HasMaxLength(100);
        builder.Property(x => x.ShippingMethodName).HasMaxLength(200);
        builder.Property(x => x.ShippingEstimatedDelivery).HasMaxLength(200);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.ShippingAmount).HasPrecision(18, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.VatAmount).HasPrecision(18, 2);
        builder.Property(x => x.VatPercent).HasPrecision(5, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.CouponCode).HasMaxLength(50);
        builder.Property(x => x.PaymentProvider).HasMaxLength(64);
        builder.Property(x => x.PaymentTransactionId).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.AdminNotes).HasMaxLength(2000);
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.PaymentExpiresAtUtc);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.Status, x.PaymentStatus });

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Lines).HasField("_lines");
        builder.Metadata.FindNavigation(nameof(Order.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductTitle).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Sku).HasMaxLength(100);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Ignore(x => x.LineTotal);
    }
}

public sealed class ShopSettingsConfiguration : IEntityTypeConfiguration<ShopSettings>
{
    public void Configure(EntityTypeBuilder<ShopSettings> builder)
    {
        builder.ToTable("Settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Mode).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.SalesAudience).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.WholesaleMinimumOrderAmount).HasPrecision(18, 2);
        builder.Property(x => x.VatPercent).HasPrecision(5, 2);
        builder.Property(x => x.BankAccountHolderName).HasMaxLength(200);
        builder.Property(x => x.BankName).HasMaxLength(100);
        builder.Property(x => x.BankCardNumber).HasMaxLength(32);
        builder.Property(x => x.BankShebaNumber).HasMaxLength(34);
        builder.Property(x => x.BankTransferInstructions).HasMaxLength(2000);
        builder.Property(x => x.SellerName).HasMaxLength(200);
        builder.Property(x => x.SellerPhone).HasMaxLength(40);
        builder.Property(x => x.SellerProvince).HasMaxLength(100);
        builder.Property(x => x.SellerCity).HasMaxLength(100);
        builder.Property(x => x.SellerAddress).HasMaxLength(500);
        builder.Property(x => x.SellerPostalCode).HasMaxLength(10);
        builder.Property(x => x.SellerEmail).HasMaxLength(256);
        builder.Property(x => x.SellerNationalId).HasMaxLength(11);
        builder.Property(x => x.SellerEconomicCode).HasMaxLength(14);
        builder.Property(x => x.SellerRegistrationNumber).HasMaxLength(50);
        builder.Property(x => x.InvoiceSellerDisplayFields).HasConversion<int>();
        builder.Property(x => x.InvoiceBuyerDisplayFields).HasConversion<int>();
        builder.Ignore(x => x.HasBankTransferAccount);
    }
}

public sealed class PaymentProviderConfigConfiguration : IEntityTypeConfiguration<PaymentProviderConfig>
{
    public void Configure(EntityTypeBuilder<PaymentProviderConfig> builder)
    {
        builder.ToTable("PaymentProviderConfigs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProviderType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EncryptedSettings).IsRequired();
        builder.HasIndex(x => x.ProviderType).IsUnique();
        builder.HasIndex(x => x.SortOrder);
    }
}

public sealed class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.ToTable("PaymentAttempts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProviderType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ReferenceId).HasMaxLength(200);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CallbackPayload).HasMaxLength(4000);
        builder.Property(x => x.TransactionId).HasMaxLength(200);
        builder.Property(x => x.FailureMessage).HasMaxLength(1000);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.ReferenceId);
        builder.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("CustomerAddresses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.RecipientName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RecipientPhone).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Province).HasMaxLength(100).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(20);
        builder.Property(x => x.AddressLine).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.IsDefault });
    }
}
