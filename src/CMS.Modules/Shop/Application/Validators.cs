using FluentValidation;
using CMS.Modules.Shop.Application.Cart;
using CMS.Modules.Shop.Application.Catalog;
using CMS.Modules.Shop.Application.Categories;
using CMS.Modules.Shop.Application.Payments;
using CMS.Modules.Shop.Application.Orders;
using CMS.Modules.Shop.Application.Pricing;
using CMS.Modules.Shop.Application.Products;
using CMS.Modules.Shop.Application.Promotions;
using CMS.Modules.Shop.Application.Reviews;
using CMS.Modules.Shop.Application.Settings;
using CMS.Modules.Shop.Application.Shipping;
using CMS.Modules.Shop.Application.Wholesale;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application;

public sealed class SaveShopCategoryCommandValidator : AbstractValidator<SaveShopCategoryCommand>
{
    public SaveShopCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Slug));
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Content).MaximumLength(500_000);
        RuleFor(x => x.ImageUrl).MaximumLength(1000);
        RuleFor(x => x.MetaTitle).MaximumLength(200);
        RuleFor(x => x.MetaDescription).MaximumLength(500);
        RuleFor(x => x.SeoKeywords).MaximumLength(500);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class SaveProductCommandValidator : AbstractValidator<SaveProductCommand>
{
    public SaveProductCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Slug).MaximumLength(300).When(x => !string.IsNullOrWhiteSpace(x.Slug));
        RuleFor(x => x.ShortDescription).MaximumLength(2000);
        RuleFor(x => x.Description).MaximumLength(20_000);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0).When(x => x.SalePrice.HasValue);
        RuleFor(x => x.Currency).MaximumLength(8);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.CoverImageUrl).MaximumLength(1000);
        RuleFor(x => x.LowStockThreshold).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Weight).GreaterThanOrEqualTo(0).When(x => x.Weight.HasValue);
        RuleFor(x => x.MinimumOrderQuantity).GreaterThanOrEqualTo(1);
        RuleFor(x => x.WholesaleMinimumOrderQuantity)
            .GreaterThanOrEqualTo(1)
            .When(x => x.WholesaleMinimumOrderQuantity.HasValue);
        RuleFor(x => x.WholesaleMinimumOrderAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.WholesaleMinimumOrderAmount.HasValue);
        RuleFor(x => x.Sku).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Sku));
    }
}

public sealed class SaveProductVariationCommandValidator : AbstractValidator<SaveProductVariationCommand>
{
    public SaveProductVariationCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AttributeSummary).MaximumLength(500);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0).When(x => x.SalePrice.HasValue);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).When(x => x.StockQuantity.HasValue);
        RuleFor(x => x.ImageUrl).MaximumLength(1000);
        RuleFor(x => x.Weight).GreaterThanOrEqualTo(0).When(x => x.Weight.HasValue);
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class UpdateShopModeCommandValidator : AbstractValidator<UpdateShopModeCommand>
{
    public UpdateShopModeCommandValidator() => RuleFor(x => x.Mode).IsInEnum();
}

public sealed class UpdateShopCommerceSettingsCommandValidator : AbstractValidator<UpdateShopCommerceSettingsCommand>
{
    public UpdateShopCommerceSettingsCommandValidator()
    {
        RuleFor(x => x.SalesAudience).IsInEnum();
        RuleFor(x => x.PendingPaymentTimeoutMinutes)
            .InclusiveBetween(1, 24 * 60)
            .WithMessage("مهلت پرداخت باید بین ۱ تا ۱۴۴۰ دقیقه باشد.");
        RuleFor(x => x.AbandonedPaymentReminderMinutes)
            .InclusiveBetween(1, 24 * 60)
            .WithMessage("زمان یادآوری پرداخت باید بین ۱ تا ۱۴۴۰ دقیقه باشد.");
        RuleFor(x => x.VatPercent)
            .InclusiveBetween(0, 100)
            .WithMessage("درصد مالیات بر ارزش افزوده باید بین ۰ تا ۱۰۰ باشد.");
        RuleFor(x => x)
            .Must(x => x.AbandonedPaymentReminderMinutes < x.PendingPaymentTimeoutMinutes)
            .WithMessage("زمان یادآوری باید کمتر از مهلت پرداخت باشد.");
        RuleFor(x => x.BankAccountHolderName).MaximumLength(200);
        RuleFor(x => x.BankName).MaximumLength(100);
        RuleFor(x => x.BankCardNumber).MaximumLength(32);
        RuleFor(x => x.BankShebaNumber).MaximumLength(34);
        RuleFor(x => x.BankTransferInstructions).MaximumLength(2000);
        When(x => x.EnableBankTransfer, () =>
        {
            RuleFor(x => x.BankAccountHolderName)
                .NotEmpty()
                .WithMessage("نام صاحب حساب برای واریز بانکی الزامی است.");
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.BankCardNumber) || !string.IsNullOrWhiteSpace(x.BankShebaNumber))
                .WithMessage("حداقل شماره کارت یا شبا برای واریز بانکی الزامی است.");
        });
    }
}

public sealed class UpdateShopSellerSettingsCommandValidator : AbstractValidator<UpdateShopSellerSettingsCommand>
{
    public UpdateShopSellerSettingsCommandValidator()
    {
        RuleFor(x => x.SellerName).MaximumLength(200);
        RuleFor(x => x.SellerPhone).MaximumLength(40);
        RuleFor(x => x.SellerEmail).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.SellerEmail));
        RuleFor(x => x.SellerProvince).MaximumLength(100);
        RuleFor(x => x.SellerCity).MaximumLength(100);
        RuleFor(x => x.SellerAddress).MaximumLength(500);
        RuleFor(x => x.SellerPostalCode).MaximumLength(10);
        RuleFor(x => x.SellerNationalId).MaximumLength(11);
        RuleFor(x => x.SellerEconomicCode).MaximumLength(14);
        RuleFor(x => x.SellerRegistrationNumber).MaximumLength(50);
    }
}

public sealed class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.CustomerPhone).MaximumLength(40);
        RuleFor(x => x.AddressId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.CallbackBaseUrl).NotEmpty().When(x =>
            x.PaymentMethod is PaymentMethod.Online or PaymentMethod.SnapPay);
        RuleFor(x => x.PaymentProviderConfigId)
            .NotEmpty()
            .When(x => x.PaymentMethod == PaymentMethod.Online);
    }
}

public sealed class SavePaymentProviderCommandValidator : AbstractValidator<SavePaymentProviderCommand>
{
    public SavePaymentProviderCommandValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class ChangeOrderStatusCommandValidator : AbstractValidator<ChangeOrderStatusCommand>
{
    public ChangeOrderStatusCommandValidator() => RuleFor(x => x.Status).IsInEnum();
}

public sealed class SaveBrandCommandValidator : AbstractValidator<SaveBrandCommand>
{
    public SaveBrandCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Content).MaximumLength(500_000);
        RuleFor(x => x.LogoUrl).MaximumLength(1000);
        RuleFor(x => x.MetaTitle).MaximumLength(200);
        RuleFor(x => x.MetaDescription).MaximumLength(500);
        RuleFor(x => x.SeoKeywords).MaximumLength(500);
    }
}

public sealed class SaveCouponCommandValidator : AbstractValidator<SaveCouponCommand>
{
    public SaveCouponCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DiscountType).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("مقدار تخفیف باید بیشتر از صفر باشد.");
        RuleFor(x => x.Amount)
            .LessThanOrEqualTo(100)
            .When(x => x.DiscountType == DiscountType.Percentage)
            .WithMessage("برای تخفیف درصدی، مقدار باید حداکثر ۱۰۰ باشد.");
        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinOrderAmount.HasValue);
        RuleFor(x => x.EndsAtUtc)
            .GreaterThanOrEqualTo(x => x.StartsAtUtc)
            .When(x => x.StartsAtUtc.HasValue && x.EndsAtUtc.HasValue)
            .WithMessage("تاریخ پایان باید بعد از تاریخ شروع باشد.");
        RuleFor(x => x.AllowedUserId)
            .MaximumLength(450)
            .When(x => !string.IsNullOrWhiteSpace(x.AllowedUserId));
    }
}

public sealed class SaveShippingMethodCommandValidator : AbstractValidator<SaveShippingMethodCommand>
{
    public SaveShippingMethodCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.EstimatedDeliveryText).MaximumLength(200);
        RuleFor(x => x.CalculationType).IsInEnum();
        RuleFor(x => x.FixedCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FreeShippingMinAmount).GreaterThanOrEqualTo(0).When(x => x.FreeShippingMinAmount.HasValue);
    }
}

public sealed class SubmitWholesaleRequestCommandValidator : AbstractValidator<SubmitWholesaleRequestCommand>
{
    public SubmitWholesaleRequestCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}

public sealed class SubmitReviewCommandValidator : AbstractValidator<SubmitReviewCommand>
{
    public SubmitReviewCommandValidator()
    {
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(4000);
    }
}

public sealed class SaveCustomerGroupCommandValidator : AbstractValidator<SaveCustomerGroupCommand>
{
    public SaveCustomerGroupCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
    }
}

public sealed class SavePriceRuleCommandValidator : AbstractValidator<SavePriceRuleCommand>
{
    public SavePriceRuleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RuleType).IsInEnum();
    }
}
