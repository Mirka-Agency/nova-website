using CMS.Modules.Shop.Application;
using CMS.Modules.Shop.Application.Orders;
using CMS.Modules.Shop.Application.Settings;
using CMS.Modules.Shop.Domain.Enums;
using FluentAssertions;

namespace CMS.Modules.Shop.Application.Tests;

public class CheckoutCommandValidatorTests
{
    private readonly CheckoutCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.Validate(new CheckoutCommand(
            "Ali",
            "ali@example.com",
            null,
            Guid.NewGuid(),
            null,
            PaymentMethod.BankTransfer,
            null,
            null,
            "user-1",
            "https://example.com"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Missing_Email_Fails()
    {
        var result = _validator.Validate(new CheckoutCommand(
            "Ali",
            "",
            null,
            Guid.NewGuid(),
            null,
            PaymentMethod.Online,
            null,
            Guid.NewGuid(),
            "user-1",
            "https://example.com"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutCommand.CustomerEmail));
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var result = _validator.Validate(new CheckoutCommand(
            "Ali",
            "not-an-email",
            null,
            Guid.NewGuid(),
            null,
            PaymentMethod.Online,
            null,
            Guid.NewGuid(),
            "user-1",
            "https://example.com"));
        result.IsValid.Should().BeFalse();
    }
}

public class UpdateShopModeCommandValidatorTests
{
    private readonly UpdateShopModeCommandValidator _validator = new();

    [Theory]
    [InlineData(ShopMode.CatalogOnly)]
    [InlineData(ShopMode.OnlineStore)]
    [InlineData(ShopMode.Hybrid)]
    public void Defined_Modes_Pass(ShopMode mode)
    {
        var result = _validator.Validate(new UpdateShopModeCommand(mode));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Undefined_Mode_Fails()
    {
        var result = _validator.Validate(new UpdateShopModeCommand((ShopMode)999));
        result.IsValid.Should().BeFalse();
    }
}
