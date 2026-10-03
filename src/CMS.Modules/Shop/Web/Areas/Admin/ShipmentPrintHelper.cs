namespace CMS.Modules.Shop.Web.Areas.Admin;

internal static class ShipmentPrintHelper
{
    public static (string? Address, string? PostalCode) SplitPostalCode(string? combined)
    {
        if (string.IsNullOrWhiteSpace(combined))
            return (combined, null);

        var parts = combined.Split('،', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1 && IsIranPostalCode(parts[^1]))
        {
            var postal = parts[^1];
            var address = string.Join("، ", parts[..^1]);
            return (string.IsNullOrWhiteSpace(address) ? null : address, postal);
        }

        return (combined, null);
    }

    private static bool IsIranPostalCode(string value) =>
        value.Length == 10 && value.All(char.IsDigit);
}
