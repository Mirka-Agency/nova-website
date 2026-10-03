using CMS.Modules.Shop.Application.Settings;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application.Invoices;

public static class InvoicePrintFieldBuilder
{
    public static IReadOnlyList<InvoicePartyFieldDto> BuildSellerFields(
        InvoiceSellerDisplayFields displayFields,
        string? name,
        string? nationalId,
        string? economicCode,
        string? registrationNumber,
        string? phone,
        string? email,
        string? province,
        string? city,
        string? postalCode,
        string? address)
    {
        var rows = new List<InvoicePartyFieldDto>();

        if (displayFields.HasFlag(InvoiceSellerDisplayFields.Name))
            rows.Add(new("SellerName", ValueOrDash(name)));
        if (displayFields.HasFlag(InvoiceSellerDisplayFields.NationalId))
            rows.Add(new("SellerNationalId", ValueOrDash(nationalId), true));
        if (displayFields.HasFlag(InvoiceSellerDisplayFields.EconomicCode))
            rows.Add(new("SellerEconomicCode", ValueOrDash(economicCode), true));
        if (displayFields.HasFlag(InvoiceSellerDisplayFields.RegistrationNumber))
            rows.Add(new("SellerRegistrationNumber", ValueOrDash(registrationNumber), true));
        if (displayFields.HasFlag(InvoiceSellerDisplayFields.Phone))
            rows.Add(new("SellerPhone", ValueOrDash(phone), true));
        if (displayFields.HasFlag(InvoiceSellerDisplayFields.Email))
            rows.Add(new("SellerEmail", ValueOrDash(email), true));
        if (displayFields.HasFlag(InvoiceSellerDisplayFields.City))
        {
            var location = string.Join("، ", new[] { province, city }.Where(s => !string.IsNullOrWhiteSpace(s)));
            rows.Add(new("ShippingCity", ValueOrDash(string.IsNullOrWhiteSpace(location) ? null : location)));
        }
        if (displayFields.HasFlag(InvoiceSellerDisplayFields.PostalCode))
            rows.Add(new("PostalCode", ValueOrDash(postalCode), true));
        if (displayFields.HasFlag(InvoiceSellerDisplayFields.Address))
            rows.Add(new("SellerAddress", ValueOrDash(address)));

        return rows;
    }

    public static IReadOnlyList<InvoicePartyFieldDto> BuildBuyerFields(
        InvoiceBuyerDisplayFields displayFields,
        string customerName,
        string customerEmail,
        string? customerPhone,
        string? recipientName,
        string? recipientPhone,
        string? shippingCity,
        string? shippingAddress,
        bool isWholesale)
    {
        var rows = new List<InvoicePartyFieldDto>();

        if (displayFields.HasFlag(InvoiceBuyerDisplayFields.CustomerName))
            rows.Add(new("CustomerName", ValueOrDash(customerName)));
        if (displayFields.HasFlag(InvoiceBuyerDisplayFields.CustomerEmail))
            rows.Add(new("CustomerEmail", ValueOrDash(customerEmail), true));
        if (displayFields.HasFlag(InvoiceBuyerDisplayFields.CustomerPhone))
            rows.Add(new("CustomerPhone", ValueOrDash(customerPhone), true));
        if (displayFields.HasFlag(InvoiceBuyerDisplayFields.RecipientName))
            rows.Add(new("RecipientName", ValueOrDash(recipientName)));
        if (displayFields.HasFlag(InvoiceBuyerDisplayFields.RecipientPhone))
            rows.Add(new("RecipientPhone", ValueOrDash(recipientPhone), true));
        if (displayFields.HasFlag(InvoiceBuyerDisplayFields.ShippingCity))
            rows.Add(new("ShippingCity", ValueOrDash(shippingCity)));
        if (displayFields.HasFlag(InvoiceBuyerDisplayFields.ShippingAddress))
            rows.Add(new("ShippingAddress", ValueOrDash(shippingAddress)));
        if (displayFields.HasFlag(InvoiceBuyerDisplayFields.IsWholesale) && isWholesale)
            rows.Add(new("IsWholesale", "Yes"));

        return rows;
    }

    private static string ValueOrDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
}
