using CMS.Application.Common.Features;
using CMS.Application.Payments;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Settings;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Web.Areas.Admin.ViewModels;
using CMS.Modules.Shop.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Shop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewShop")]
public class ShopSettingsController : Controller
{
    private readonly IShopSettingsService _settings;
    private readonly IPaymentGateway _payments;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public ShopSettingsController(
        IShopSettingsService settings,
        IPaymentGateway payments,
        IFeatureManager features,
        IStringLocalizer<ShopAdmin> localizer)
    {
        _settings = settings;
        _payments = payments;
        _features = features;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(string? tab, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["ShopSettings"].Value;
        var dto = await _settings.GetAsync(cancellationToken);
        return View(BuildViewModel(dto, NormalizeTab(tab)));
    }

    [HttpPost]
    public async Task<IActionResult> Index(ShopSettingsViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["ShopSettings"].Value;
        var tab = NormalizeTab(model.ActiveTab);
        try
        {
            if (tab == "commerce")
            {
                await _settings.UpdateCommerceAsync(new UpdateShopCommerceSettingsCommand(
                    model.SalesAudience,
                    model.EnableOnlinePayment,
                    model.EnableBankTransfer,
                    model.BankAccountHolderName,
                    model.BankName,
                    model.BankCardNumber,
                    model.BankShebaNumber,
                    model.BankTransferInstructions,
                    model.ShowPricesToGuests,
                    model.EnableReviews,
                    model.EnableCoupons,
                    model.EnableWholesaleInvoice,
                    model.WholesaleMinimumOrderAmount,
                    model.WholesaleMinimumOrderQuantity,
                    model.PendingPaymentTimeoutMinutes,
                    model.AbandonedPaymentReminderMinutes,
                    model.EnableVat,
                    model.VatPercent), cancellationToken);
            }
            else if (tab == "seller")
            {
                await _settings.UpdateSellerAsync(new UpdateShopSellerSettingsCommand(
                    model.SellerName,
                    model.SellerPhone,
                    model.SellerEmail,
                    model.SellerProvince,
                    model.SellerCity,
                    model.SellerAddress,
                    model.SellerPostalCode,
                    model.SellerNationalId,
                    model.SellerEconomicCode,
                    model.SellerRegistrationNumber,
                    InvoiceDisplayFieldFormMapper.ToSellerFlags(model),
                    InvoiceDisplayFieldFormMapper.ToBuyerFlags(model)), cancellationToken);
            }
            else
            {
                await _settings.UpdateModeAsync(new UpdateShopModeCommand(model.Mode), cancellationToken);
            }

            TempData["Success"] = _localizer["SettingsUpdated"].Value;
            return RedirectToAction(nameof(Index), new { tab });
        }
        catch (ValidationException ex)
        {
            foreach (var (key, messages) in ex.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(key, message);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        var dto = await _settings.GetAsync(cancellationToken);
        var vm = BuildViewModel(dto, tab);
        vm.Mode = model.Mode;
        vm.SalesAudience = model.SalesAudience;
        vm.EnableOnlinePayment = model.EnableOnlinePayment;
        vm.EnableBankTransfer = model.EnableBankTransfer;
        vm.BankAccountHolderName = model.BankAccountHolderName;
        vm.BankName = model.BankName;
        vm.BankCardNumber = model.BankCardNumber;
        vm.BankShebaNumber = model.BankShebaNumber;
        vm.BankTransferInstructions = model.BankTransferInstructions;
        vm.ShowPricesToGuests = model.ShowPricesToGuests;
        vm.EnableReviews = model.EnableReviews;
        vm.EnableCoupons = model.EnableCoupons;
        vm.EnableWholesaleInvoice = model.EnableWholesaleInvoice;
        vm.WholesaleMinimumOrderAmount = model.WholesaleMinimumOrderAmount;
        vm.WholesaleMinimumOrderQuantity = model.WholesaleMinimumOrderQuantity;
        vm.PendingPaymentTimeoutMinutes = model.PendingPaymentTimeoutMinutes;
        vm.AbandonedPaymentReminderMinutes = model.AbandonedPaymentReminderMinutes;
        vm.EnableVat = model.EnableVat;
        vm.VatPercent = model.VatPercent;
        vm.SellerName = model.SellerName;
        vm.SellerPhone = model.SellerPhone;
        vm.SellerEmail = model.SellerEmail;
        vm.SellerProvince = model.SellerProvince;
        vm.SellerCity = model.SellerCity;
        vm.SellerAddress = model.SellerAddress;
        vm.SellerPostalCode = model.SellerPostalCode;
        vm.SellerNationalId = model.SellerNationalId;
        vm.SellerEconomicCode = model.SellerEconomicCode;
        vm.SellerRegistrationNumber = model.SellerRegistrationNumber;
        vm.InvoiceShowSellerName = model.InvoiceShowSellerName;
        vm.InvoiceShowSellerNationalId = model.InvoiceShowSellerNationalId;
        vm.InvoiceShowSellerEconomicCode = model.InvoiceShowSellerEconomicCode;
        vm.InvoiceShowSellerRegistrationNumber = model.InvoiceShowSellerRegistrationNumber;
        vm.InvoiceShowSellerPhone = model.InvoiceShowSellerPhone;
        vm.InvoiceShowSellerEmail = model.InvoiceShowSellerEmail;
        vm.InvoiceShowSellerCity = model.InvoiceShowSellerCity;
        vm.InvoiceShowSellerPostalCode = model.InvoiceShowSellerPostalCode;
        vm.InvoiceShowSellerAddress = model.InvoiceShowSellerAddress;
        vm.InvoiceShowBuyerCustomerName = model.InvoiceShowBuyerCustomerName;
        vm.InvoiceShowBuyerCustomerEmail = model.InvoiceShowBuyerCustomerEmail;
        vm.InvoiceShowBuyerCustomerPhone = model.InvoiceShowBuyerCustomerPhone;
        vm.InvoiceShowBuyerRecipientName = model.InvoiceShowBuyerRecipientName;
        vm.InvoiceShowBuyerRecipientPhone = model.InvoiceShowBuyerRecipientPhone;
        vm.InvoiceShowBuyerShippingCity = model.InvoiceShowBuyerShippingCity;
        vm.InvoiceShowBuyerShippingAddress = model.InvoiceShowBuyerShippingAddress;
        vm.InvoiceShowBuyerIsWholesale = model.InvoiceShowBuyerIsWholesale;
        return View(vm);
    }

    private ShopSettingsViewModel BuildViewModel(ShopSettingsDto dto, string tab)
    {
        var vm = new ShopSettingsViewModel
        {
            Mode = dto.Mode,
            ModeDisplay = ModeLabel(dto.Mode),
            IsCatalogOnly = dto.IsCatalogOnly,
            EcommerceEnabled = dto.EcommerceEnabled,
            ActiveTab = tab,
            PaymentProviderName = _payments.ProviderName,
            SalesAudience = dto.SalesAudience,
            EnableOnlinePayment = dto.EnableOnlinePayment,
            EnableBankTransfer = dto.EnableBankTransfer,
            BankAccountHolderName = dto.BankAccountHolderName,
            BankName = dto.BankName,
            BankCardNumber = dto.BankCardNumber,
            BankShebaNumber = dto.BankShebaNumber,
            BankTransferInstructions = dto.BankTransferInstructions,
            ShowPricesToGuests = dto.ShowPricesToGuests,
            EnableReviews = dto.EnableReviews,
            EnableCoupons = dto.EnableCoupons,
            EnableWholesaleInvoice = dto.EnableWholesaleInvoice,
            WholesaleMinimumOrderAmount = dto.WholesaleMinimumOrderAmount,
            WholesaleMinimumOrderQuantity = dto.WholesaleMinimumOrderQuantity,
            PendingPaymentTimeoutMinutes = dto.PendingPaymentTimeoutMinutes,
            AbandonedPaymentReminderMinutes = dto.AbandonedPaymentReminderMinutes,
            EnableVat = dto.EnableVat,
            VatPercent = dto.VatPercent,
            SellerName = dto.SellerName,
            SellerPhone = dto.SellerPhone,
            SellerEmail = dto.SellerEmail,
            SellerProvince = dto.SellerProvince,
            SellerCity = dto.SellerCity,
            SellerAddress = dto.SellerAddress,
            SellerPostalCode = dto.SellerPostalCode,
            SellerNationalId = dto.SellerNationalId,
            SellerEconomicCode = dto.SellerEconomicCode,
            SellerRegistrationNumber = dto.SellerRegistrationNumber,
            ModeOptions =
            [
                new SelectListItem(ModeLabel(ShopMode.CatalogOnly), nameof(ShopMode.CatalogOnly), dto.Mode == ShopMode.CatalogOnly),
                new SelectListItem(ModeLabel(ShopMode.OnlineStore), nameof(ShopMode.OnlineStore), dto.Mode == ShopMode.OnlineStore),
                new SelectListItem(ModeLabel(ShopMode.Hybrid), nameof(ShopMode.Hybrid), dto.Mode == ShopMode.Hybrid)
            ],
            SalesAudienceOptions = Enum.GetValues<SalesAudience>()
                .Select(a => new SelectListItem(AudienceLabel(a), a.ToString(), a == dto.SalesAudience))
        };
        InvoiceDisplayFieldFormMapper.ApplyToViewModel(vm, dto.InvoiceSellerDisplayFields, dto.InvoiceBuyerDisplayFields);
        return vm;
    }

    private static string NormalizeTab(string? tab) =>
        tab?.ToLowerInvariant() switch
        {
            "payments" => "payments",
            "commerce" => "commerce",
            "seller" => "seller",
            _ => "general"
        };

    private string ModeLabel(ShopMode mode) => mode switch
    {
        ShopMode.CatalogOnly => _localizer["CatalogOnlyMode"].Value,
        ShopMode.OnlineStore => _localizer["OnlineStoreMode"].Value,
        ShopMode.Hybrid => _localizer["HybridMode"].Value,
        _ => mode.ToString()
    };

    private string AudienceLabel(SalesAudience audience) => audience switch
    {
        SalesAudience.RetailOnly => _localizer["SalesAudienceRetail"].Value,
        SalesAudience.WholesaleOnly => _localizer["SalesAudienceWholesale"].Value,
        SalesAudience.Both => _localizer["SalesAudienceBoth"].Value,
        _ => audience.ToString()
    };
}
