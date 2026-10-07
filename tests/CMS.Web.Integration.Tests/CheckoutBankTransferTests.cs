using System.Security.Claims;
using CMS.Application.Auth;
using CMS.Infrastructure.Identity;
using CMS.Modules.Shop.Application.Account;
using CMS.Modules.Shop.Application.Cart;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Orders;
using CMS.Modules.Shop.Application.Products;
using CMS.Modules.Shop.Application.Settings;
using CMS.Modules.Shop.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Web.Integration.Tests;

[Collection(CmsWebCollection.Name)]
public sealed class CheckoutBankTransferTests
{
    private readonly CmsWebFixture _fixture;

    public CheckoutBankTransferTests(CmsWebFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task BankTransfer_Checkout_Creates_Unpaid_Order_And_Clears_Cart()
    {
        _fixture.EnsureAvailable();
        await _fixture.SetShopFeatureAsync(enabled: true);

        await using var scope = _fixture.Factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var http = sp.GetRequiredService<IHttpContextAccessor>();

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var phone = $"09{Random.Shared.NextInt64(100000000, 999999999)}";
        var user = new ApplicationUser
        {
            UserName = phone,
            PhoneNumber = phone,
            PhoneNumberConfirmed = true,
            FullName = "مشتری تست",
            Email = $"{phone}@customers.local",
            EmailConfirmed = false
        };
        var create = await userManager.CreateAsync(user);
        create.Succeeded.Should().BeTrue(string.Join("; ", create.Errors.Select(e => e.Description)));

        http.HttpContext = new DefaultHttpContext
        {
            RequestServices = sp,
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.FullName!),
                new Claim(ClaimTypes.MobilePhone, phone)
            ], CustomerAuthDefaults.AuthenticationScheme))
        };

        var settings = sp.GetRequiredService<IShopSettingsService>();
        await settings.UpdateModeAsync(new UpdateShopModeCommand(ShopMode.OnlineStore));
        await settings.UpdateCommerceAsync(new UpdateShopCommerceSettingsCommand(
            SalesAudience.RetailOnly,
            EnableOnlinePayment: true,
            EnableBankTransfer: true,
            BankAccountHolderName: "فروشگاه تست",
            BankName: "ملت",
            BankCardNumber: "6037991234567890",
            BankShebaNumber: "IR120170000000123456789001",
            BankTransferInstructions: "مبلغ سفارش را واریز کنید.",
            ShowPricesToGuests: true,
            EnableReviews: true,
            EnableCoupons: true,
            EnableWholesaleInvoice: true,
            WholesaleMinimumOrderAmount: null,
            WholesaleMinimumOrderQuantity: null,
            PendingPaymentTimeoutMinutes: 30,
            AbandonedPaymentReminderMinutes: 3,
            EnableVat: false,
            VatPercent: 10m));

        var products = sp.GetRequiredService<IProductService>();
        var slug = $"chk-{Guid.NewGuid():N}"[..20];
        var productId = await products.CreateAsync(new SaveProductCommand(
            Title: $"Checkout Test {slug}",
            Slug: slug,
            ShortDescription: "test",
            Description: "test product",
            Price: 100_000m,
            SalePrice: null,
            SaleStartsAtUtc: null,
            SaleEndsAtUtc: null,
            Currency: "IRR",
            IsAvailable: true,
            IsPurchasable: true,
            Status: ProductStatus.Active,
            CategoryId: null,
            BrandId: null,
            CoverImageUrl: null,
            CoverImageAlt: null,
            VideoUrl: null,
            StockQuantity: null,
            UnlimitedStock: true,
            LowStockThreshold: 0,
            Weight: 1m,
            MinimumOrderQuantity: 1,
            WholesaleMinimumOrderQuantity: null,
            WholesaleMinimumOrderAmount: null,
            Sku: $"SKU-{slug}",
            MetaTitle: null,
            MetaDescription: null,
            SeoKeywords: null,
            CanonicalUrl: null,
            OgTitle: null,
            OgDescription: null,
            OgImageUrl: null,
            FaqJson: null,
            Images: null,
            Variations: null));

        var addresses = sp.GetRequiredService<ICustomerAddressService>();
        var addressId = await addresses.CreateAsync(user.Id, new SaveCustomerAddressCommand(
            "مشتری تست",
            phone,
            "تهران",
            "تهران",
            "1234567890",
            "خیابان تست، پلاک ۱",
            IsDefault: true));

        var cart = sp.GetRequiredService<ICartService>();
        await cart.AddAsync(new AddToCartCommand(productId, null, 1));

        var before = await cart.GetAsync();
        before.Items.Should().ContainSingle();

        var orders = sp.GetRequiredService<IOrderService>();
        var result = await orders.CheckoutAsync(new CheckoutCommand(
            user.FullName!,
            user.Email!,
            phone,
            addressId,
            Notes: null,
            PaymentMethod.BankTransfer,
            ShippingMethodId: null,
            PaymentProviderConfigId: null,
            user.Id,
            "https://localhost"));

        result.OrderId.Should().NotBeEmpty();
        result.OrderNumber.Should().MatchRegex(@"^\d{6}-\d{5}$");
        result.RedirectUrl.Should().BeNull();
        result.PaymentStatus.Should().Be(PaymentStatus.Unpaid);
        result.AwaitingOfflinePayment.Should().BeTrue();
        result.BankTransfer.Should().NotBeNull();
        result.BankTransfer!.CardNumber.Should().Be("6037991234567890");
        result.BankTransfer.ShebaNumber.Should().Be("IR120170000000123456789001");

        var after = await cart.GetAsync();
        after.Items.Should().BeEmpty();

        var detail = await orders.GetAsync(result.OrderId);
        detail.Should().NotBeNull();
        detail!.PaymentMethod.Should().Be(PaymentMethod.BankTransfer);
        detail.Lines.Should().ContainSingle();
        detail.TotalAmount.Should().BeGreaterThan(0);
    }
}
