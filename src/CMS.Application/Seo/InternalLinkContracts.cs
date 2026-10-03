namespace CMS.Application.Seo;

/// <summary>Shared content-type keys for SEO documents and internal link candidates.</summary>
public static class SeoContentTypeKeys
{
    public const string BlogPost = "blog-post";
    public const string NewsArticle = "news-article";
    public const string ServiceItem = "service-item";
    public const string VideoItem = "video-item";
    public const string TeamItem = "team-item";
    public const string ShopProduct = "shop-product";
}

public sealed record InternalLinkCandidate(
    string Title,
    string RelativePath,
    string ContentType,
    Guid ContentId);

/// <summary>
/// Implemented by content modules (Blog, News, …) to contribute internal link suggestions for SEO tools.
/// </summary>
public interface IInternalLinkCandidateProvider
{
    Task<IReadOnlyList<InternalLinkCandidate>> SearchAsync(
        string query,
        int take,
        CancellationToken cancellationToken = default);
}

public interface IRobotsTxtBuilder
{
    Task<string> BuildAsync(string baseUrl, CancellationToken cancellationToken = default);
}
