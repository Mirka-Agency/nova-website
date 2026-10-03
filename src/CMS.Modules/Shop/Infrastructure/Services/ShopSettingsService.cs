using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Settings;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class ShopSettingsService : IShopSettingsService
{
    private const string CacheKey = "cms:shop-settings";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(45);

    private readonly ShopDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IValidator<UpdateShopModeCommand> _modeValidator;
    private readonly IValidator<UpdateShopCommerceSettingsCommand> _commerceValidator;
    private readonly IValidator<UpdateShopSellerSettingsCommand> _sellerValidator;

    public ShopSettingsService(
        ShopDbContext db,
        IMemoryCache cache,
        IValidator<UpdateShopModeCommand> modeValidator,
        IValidator<UpdateShopCommerceSettingsCommand> commerceValidator,
        IValidator<UpdateShopSellerSettingsCommand> sellerValidator)
    {
        _db = db;
        _cache = cache;
        _modeValidator = modeValidator;
        _commerceValidator = commerceValidator;
        _sellerValidator = sellerValidator;
    }

    public async Task<ShopSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out ShopSettingsDto? cached) && cached is not null)
            return cached;

        var settings = await EnsureAsync(cancellationToken);
        var dto = Map(settings);
        _cache.Set(CacheKey, dto, CacheDuration);
        return dto;
    }

    public async Task UpdateModeAsync(UpdateShopModeCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _modeValidator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var settings = await EnsureAsync(cancellationToken);
        settings.SetMode(command.Mode);
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
    }

    public async Task UpdateCommerceAsync(UpdateShopCommerceSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _commerceValidator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var settings = await EnsureAsync(cancellationToken);
        settings.UpdateCommerceOptions(
            command.SalesAudience,
            command.EnableOnlinePayment,
            command.EnableBankTransfer,
            command.BankAccountHolderName,
            command.BankName,
            command.BankCardNumber,
            command.BankShebaNumber,
            command.BankTransferInstructions,
            command.ShowPricesToGuests,
            command.EnableReviews,
            command.EnableCoupons,
            command.EnableWholesaleInvoice,
            command.WholesaleMinimumOrderAmount,
            command.WholesaleMinimumOrderQuantity,
            command.PendingPaymentTimeoutMinutes,
            command.AbandonedPaymentReminderMinutes,
            command.EnableVat,
            command.VatPercent);
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
    }

    public async Task UpdateSellerAsync(UpdateShopSellerSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _sellerValidator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var settings = await EnsureAsync(cancellationToken);
        settings.UpdateSellerInfo(
            command.SellerName,
            command.SellerPhone,
            command.SellerEmail,
            command.SellerProvince,
            command.SellerCity,
            command.SellerAddress,
            command.SellerPostalCode,
            command.SellerNationalId,
            command.SellerEconomicCode,
            command.SellerRegistrationNumber,
            command.InvoiceSellerDisplayFields,
            command.InvoiceBuyerDisplayFields);
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
    }

    private async Task<ShopSettings> EnsureAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.Settings.OrderBy(s => s.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
            return settings;

        settings = ShopSettings.CreateDefault();
        _db.Settings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static ShopSettingsDto Map(ShopSettings settings) =>
        new(
            settings.Mode,
            settings.SalesAudience,
            settings.IsCatalogOnly,
            settings.IsOnlineStore,
            settings.IsHybrid,
            settings.EcommerceEnabled,
            settings.EnableOnlinePayment,
            settings.EnableBankTransfer,
            settings.BankAccountHolderName,
            settings.BankName,
            settings.BankCardNumber,
            settings.BankShebaNumber,
            settings.BankTransferInstructions,
            settings.ShowPricesToGuests,
            settings.EnableReviews,
            settings.EnableCoupons,
            settings.EnableWholesaleInvoice,
            settings.WholesaleMinimumOrderAmount,
            settings.WholesaleMinimumOrderQuantity,
            settings.PendingPaymentTimeoutMinutes,
            settings.AbandonedPaymentReminderMinutes,
            settings.EnableVat,
            settings.VatPercent,
            settings.SellerName,
            settings.SellerPhone,
            settings.SellerProvince,
            settings.SellerCity,
            settings.SellerAddress,
            settings.SellerPostalCode,
            settings.SellerEmail,
            settings.SellerNationalId,
            settings.SellerEconomicCode,
            settings.SellerRegistrationNumber,
            InvoiceDisplayFieldDefaults.ResolveSeller(settings.InvoiceSellerDisplayFields),
            InvoiceDisplayFieldDefaults.ResolveBuyer(settings.InvoiceBuyerDisplayFields));
}
