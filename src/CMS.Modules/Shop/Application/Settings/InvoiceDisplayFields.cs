using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application.Settings;

public static class InvoiceDisplayFieldDefaults
{
    public static InvoiceSellerDisplayFields Seller => ShopSettings.DefaultInvoiceSellerDisplayFields;
    public static InvoiceBuyerDisplayFields Buyer => ShopSettings.DefaultInvoiceBuyerDisplayFields;

    public static InvoiceSellerDisplayFields ResolveSeller(InvoiceSellerDisplayFields stored) =>
        stored == InvoiceSellerDisplayFields.None ? Seller : stored;

    public static InvoiceBuyerDisplayFields ResolveBuyer(InvoiceBuyerDisplayFields stored) =>
        stored == InvoiceBuyerDisplayFields.None ? Buyer : stored;
}

public sealed record InvoicePartyFieldDto(string LabelKey, string Value, bool IsLtr = false);
