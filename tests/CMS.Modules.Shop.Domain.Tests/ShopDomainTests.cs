using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Domain.Enums;
using FluentAssertions;

namespace CMS.Modules.Shop.Domain.Tests;

public class ShopSettingsTests
{
    [Fact]
    public void CanPurchaseProduct_CatalogOnly_IsAlwaysFalse()
    {
        var settings = ShopSettings.CreateDefault();
        settings.Mode.Should().Be(ShopMode.CatalogOnly);

        settings.CanPurchaseProduct(true).Should().BeFalse();
        settings.CanPurchaseProduct(false).Should().BeFalse();
        settings.EcommerceEnabled.Should().BeFalse();
    }

    [Fact]
    public void CanPurchaseProduct_OnlineStore_IsAlwaysTrue()
    {
        var settings = ShopSettings.CreateDefault();
        settings.SetMode(ShopMode.OnlineStore);

        settings.CanPurchaseProduct(true).Should().BeTrue();
        settings.CanPurchaseProduct(false).Should().BeTrue();
        settings.EcommerceEnabled.Should().BeTrue();
    }

    [Fact]
    public void CanPurchaseProduct_Hybrid_DependsOnIsPurchasable()
    {
        var settings = ShopSettings.CreateDefault();
        settings.SetMode(ShopMode.Hybrid);

        settings.CanPurchaseProduct(true).Should().BeTrue();
        settings.CanPurchaseProduct(false).Should().BeFalse();
        settings.EcommerceEnabled.Should().BeTrue();
    }
}

public class OrderTests
{
    [Fact]
    public void Create_AddLine_MarkPaid_Works()
    {
        var order = Order.Create("ord-1", null, "Ali", "ali@example.com", null, null, null, "IRR", null, false);
        order.AddLine(Guid.NewGuid(), null, "Product", null, 1000, 2);

        order.TotalAmount.Should().Be(2000);
        order.Status.Should().Be(OrderStatus.Pending);

        order.MarkPaid("Manual", "TX-1");
        order.Status.Should().Be(OrderStatus.Processing);
        order.PaymentTransactionId.Should().Be("TX-1");
    }

    [Fact]
    public void MarkPaid_OnCancelled_Throws()
    {
        var order = Order.Create("ord-2", null, "Ali", "ali@example.com", null, null, null, "IRR", null, false);
        order.AddLine(Guid.NewGuid(), null, "Product", null, 100, 1);
        order.ChangeStatus(OrderStatus.Cancelled);

        var act = () => order.MarkPaid("Manual", "TX");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ChangeStatus_WithoutLines_Throws()
    {
        var order = Order.Create("ord-3", null, "Ali", "ali@example.com", null, null, null, "IRR", null, false);
        var act = () => order.ChangeStatus(OrderStatus.Processing);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithEmptyCustomer_Throws()
    {
        var act = () => Order.Create("ord-4", null, "", "ali@example.com", null, null, null, "IRR", null, false);
        act.Should().Throw<DomainException>();
    }
}

public class ProductTests
{
    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var product = Product.Create(
            "Title", "title", null, "desc", 10, null, null, null, "IRR",
            true, true, null, null, null, null,
            null, false, 5, null, 1, null, null, null, null, null, null, null,
            null, null, null, null);
        product.Title.Should().Be("Title");
        product.IsPurchasable.Should().BeTrue();
        product.ProductType.Should().Be(ProductType.Simple);
    }

    [Fact]
    public void Create_WithEmptyTitle_Throws()
    {
        var act = () => Product.Create(
            " ", "slug", null, null, 10, null, null, null, "IRR",
            true, false, null, null, null, null,
            null, false, 5, null, 1, null, null, null, null, null, null, null,
            null, null, null, null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithNegativePrice_Throws()
    {
        var act = () => Product.Create(
            "Title", "slug", null, null, -1, null, null, null, "IRR",
            true, false, null, null, null, null,
            null, false, 5, null, 1, null, null, null, null, null, null, null,
            null, null, null, null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddVariation_MarksProductAsVariable()
    {
        var product = Product.Create(
            "Title", "title", null, null, 10, null, null, null, "IRR",
            true, true, null, null, null, null,
            null, false, 5, null, 1, null, null, null, null, null, null, null,
            null, null, null, null);

        product.AddVariation(ProductVariation.Create(
            product.Id, "SKU-1", "Red", 12, null, 5, false, null, null));

        product.ProductType.Should().Be(ProductType.Variable);
        product.Variations.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveLastVariation_MarksProductAsSimple()
    {
        var product = Product.Create(
            "Title", "title", null, null, 10, null, null, null, "IRR",
            true, true, null, null, null, null,
            null, false, 5, null, 1, null, null, null, null, null, null, null,
            null, null, null, null);
        var variation = ProductVariation.Create(
            product.Id, "SKU-1", "Red", 12, null, 5, false, null, null);
        product.AddVariation(variation);

        product.RemoveVariation(variation.Id);

        product.ProductType.Should().Be(ProductType.Simple);
        product.Variations.Should().BeEmpty();
    }
}
