using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Web.Areas.Admin.ViewModels;

namespace CMS.Modules.Shop.Web.Areas.Admin;

internal static class InvoiceDisplayFieldFormMapper
{
    public static void ApplyToViewModel(ShopSettingsViewModel vm, InvoiceSellerDisplayFields seller, InvoiceBuyerDisplayFields buyer)
    {
        vm.InvoiceShowSellerName = seller.HasFlag(InvoiceSellerDisplayFields.Name);
        vm.InvoiceShowSellerNationalId = seller.HasFlag(InvoiceSellerDisplayFields.NationalId);
        vm.InvoiceShowSellerEconomicCode = seller.HasFlag(InvoiceSellerDisplayFields.EconomicCode);
        vm.InvoiceShowSellerRegistrationNumber = seller.HasFlag(InvoiceSellerDisplayFields.RegistrationNumber);
        vm.InvoiceShowSellerPhone = seller.HasFlag(InvoiceSellerDisplayFields.Phone);
        vm.InvoiceShowSellerEmail = seller.HasFlag(InvoiceSellerDisplayFields.Email);
        vm.InvoiceShowSellerCity = seller.HasFlag(InvoiceSellerDisplayFields.City);
        vm.InvoiceShowSellerPostalCode = seller.HasFlag(InvoiceSellerDisplayFields.PostalCode);
        vm.InvoiceShowSellerAddress = seller.HasFlag(InvoiceSellerDisplayFields.Address);

        vm.InvoiceShowBuyerCustomerName = buyer.HasFlag(InvoiceBuyerDisplayFields.CustomerName);
        vm.InvoiceShowBuyerCustomerEmail = buyer.HasFlag(InvoiceBuyerDisplayFields.CustomerEmail);
        vm.InvoiceShowBuyerCustomerPhone = buyer.HasFlag(InvoiceBuyerDisplayFields.CustomerPhone);
        vm.InvoiceShowBuyerRecipientName = buyer.HasFlag(InvoiceBuyerDisplayFields.RecipientName);
        vm.InvoiceShowBuyerRecipientPhone = buyer.HasFlag(InvoiceBuyerDisplayFields.RecipientPhone);
        vm.InvoiceShowBuyerShippingCity = buyer.HasFlag(InvoiceBuyerDisplayFields.ShippingCity);
        vm.InvoiceShowBuyerShippingAddress = buyer.HasFlag(InvoiceBuyerDisplayFields.ShippingAddress);
        vm.InvoiceShowBuyerIsWholesale = buyer.HasFlag(InvoiceBuyerDisplayFields.IsWholesale);
    }

    public static InvoiceSellerDisplayFields ToSellerFlags(ShopSettingsViewModel vm)
    {
        var flags = InvoiceSellerDisplayFields.None;
        if (vm.InvoiceShowSellerName) flags |= InvoiceSellerDisplayFields.Name;
        if (vm.InvoiceShowSellerNationalId) flags |= InvoiceSellerDisplayFields.NationalId;
        if (vm.InvoiceShowSellerEconomicCode) flags |= InvoiceSellerDisplayFields.EconomicCode;
        if (vm.InvoiceShowSellerRegistrationNumber) flags |= InvoiceSellerDisplayFields.RegistrationNumber;
        if (vm.InvoiceShowSellerPhone) flags |= InvoiceSellerDisplayFields.Phone;
        if (vm.InvoiceShowSellerEmail) flags |= InvoiceSellerDisplayFields.Email;
        if (vm.InvoiceShowSellerCity) flags |= InvoiceSellerDisplayFields.City;
        if (vm.InvoiceShowSellerPostalCode) flags |= InvoiceSellerDisplayFields.PostalCode;
        if (vm.InvoiceShowSellerAddress) flags |= InvoiceSellerDisplayFields.Address;
        return flags;
    }

    public static InvoiceBuyerDisplayFields ToBuyerFlags(ShopSettingsViewModel vm)
    {
        var flags = InvoiceBuyerDisplayFields.None;
        if (vm.InvoiceShowBuyerCustomerName) flags |= InvoiceBuyerDisplayFields.CustomerName;
        if (vm.InvoiceShowBuyerCustomerEmail) flags |= InvoiceBuyerDisplayFields.CustomerEmail;
        if (vm.InvoiceShowBuyerCustomerPhone) flags |= InvoiceBuyerDisplayFields.CustomerPhone;
        if (vm.InvoiceShowBuyerRecipientName) flags |= InvoiceBuyerDisplayFields.RecipientName;
        if (vm.InvoiceShowBuyerRecipientPhone) flags |= InvoiceBuyerDisplayFields.RecipientPhone;
        if (vm.InvoiceShowBuyerShippingCity) flags |= InvoiceBuyerDisplayFields.ShippingCity;
        if (vm.InvoiceShowBuyerShippingAddress) flags |= InvoiceBuyerDisplayFields.ShippingAddress;
        if (vm.InvoiceShowBuyerIsWholesale) flags |= InvoiceBuyerDisplayFields.IsWholesale;
        return flags;
    }
}
