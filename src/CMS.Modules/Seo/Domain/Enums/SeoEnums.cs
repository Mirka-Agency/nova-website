namespace CMS.Modules.Seo.Domain.Enums;

public static class SeoContentTypes
{
    public const string BlogPost = "blog-post";
    public const string NewsArticle = "news-article";
    public const string ServiceItem = "service-item";
    public const string VideoItem = "video-item";
    public const string TeamItem = "team-item";
    public const string ShopProduct = "shop-product";

    public static readonly HashSet<string> Known =
    [
        BlogPost,
        NewsArticle,
        ServiceItem,
        VideoItem,
        TeamItem,
        ShopProduct
    ];

    public static bool IsKnown(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Known.Contains(value.Trim().ToLowerInvariant());

    public static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}

public static class SeoSchemaTypes
{
    public const string Article = "Article";
    public const string NewsArticle = "NewsArticle";
    public const string WebPage = "WebPage";
    public const string FAQPage = "FAQPage";
    public const string Product = "Product";
    public const string Organization = "Organization";
    public const string Person = "Person";
    public const string Service = "Service";

    public static readonly HashSet<string> Known =
    [
        Article,
        NewsArticle,
        WebPage,
        FAQPage,
        Product,
        Organization,
        Person,
        Service
    ];

    public static bool IsKnown(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Known.Contains(value.Trim());

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return Known.FirstOrDefault(k => k.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
    }
}
