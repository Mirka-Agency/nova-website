namespace CMS.Application.Seo;

/// <summary>
/// Registers the single page LCP image for early <c>&lt;link rel="preload" as="image"&gt;</c> in the layout.
/// Only the first registration wins so below-the-fold images never steal priority.
/// </summary>
public static class LcpImageHints
{
    public const string UrlKey = "LcpImageUrl";
    public const string SrcSetKey = "LcpImageSrcSet";
    public const string SizesKey = "LcpImageSizes";

    /// <summary>
    /// Registers an LCP candidate if one has not already been registered for this request.
    /// </summary>
    public static bool TryRegister(
        IDictionary<string, object?> viewData,
        string? url,
        string? srcSet = null,
        string? sizes = null)
    {
        ArgumentNullException.ThrowIfNull(viewData);

        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (viewData.TryGetValue(UrlKey, out var existing) && existing is string s && !string.IsNullOrWhiteSpace(s))
            return false;

        viewData[UrlKey] = url.Trim();

        if (!string.IsNullOrWhiteSpace(srcSet))
            viewData[SrcSetKey] = srcSet.Trim();

        if (!string.IsNullOrWhiteSpace(sizes))
            viewData[SizesKey] = sizes.Trim();

        return true;
    }

    public static string? GetUrl(IDictionary<string, object?> viewData) =>
        GetString(viewData, UrlKey);

    public static string? GetSrcSet(IDictionary<string, object?> viewData) =>
        GetString(viewData, SrcSetKey);

    public static string? GetSizes(IDictionary<string, object?> viewData) =>
        GetString(viewData, SizesKey);

    private static string? GetString(IDictionary<string, object?> viewData, string key)
    {
        if (!viewData.TryGetValue(key, out var value) || value is not string text)
            return null;

        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }
}
