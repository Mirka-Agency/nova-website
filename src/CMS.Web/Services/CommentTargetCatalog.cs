using CMS.Modules.Blog.Application.Interfaces;
using CMS.Modules.Blog.Domain.Enums;
using CMS.Modules.Comments.Application.Import;
using CMS.Modules.Comments.Domain.Enums;
using CMS.Modules.News.Application.Interfaces;
using CMS.Modules.News.Domain.Enums;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Web.Services;

public sealed class CommentTargetCatalog : ICommentTargetCatalog
{
    private readonly IPostService _posts;
    private readonly IArticleService _articles;
    private readonly IProductService _products;
    private readonly IShopCategoryService _categories;

    public CommentTargetCatalog(
        IPostService posts,
        IArticleService articles,
        IProductService products,
        IShopCategoryService categories)
    {
        _posts = posts;
        _articles = articles;
        _products = products;
        _categories = categories;
    }

    public async Task<IReadOnlyList<CommentTargetCandidate>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<CommentTargetCandidate>();

        var posts = await _posts.ListAsync(cancellationToken);
        foreach (var post in posts.Where(p => p.Status == PostStatus.Published))
        {
            if (string.IsNullOrWhiteSpace(post.Slug))
                continue;
            list.Add(new CommentTargetCandidate(
                CommentTargetType.BlogPost,
                post.Id,
                post.Title,
                "/" + post.Slug.Trim().Trim('/').ToLowerInvariant()));
        }

        var articles = await _articles.ListAsync(cancellationToken);
        foreach (var article in articles.Where(a =>
                     a.Kind == ArticleKind.Event && a.Status == ArticleStatus.Published))
        {
            if (string.IsNullOrWhiteSpace(article.Slug))
                continue;
            list.Add(new CommentTargetCandidate(
                CommentTargetType.Event,
                article.Id,
                article.Title,
                "/event/" + article.Slug.Trim().Trim('/').ToLowerInvariant()));
        }

        var products = await _products.ListAsync(cancellationToken);
        foreach (var product in products.Where(p => p.Status == ProductStatus.Active))
        {
            if (string.IsNullOrWhiteSpace(product.Slug))
                continue;
            list.Add(new CommentTargetCandidate(
                CommentTargetType.Product,
                product.Id,
                product.Title,
                "/shop/" + product.Slug.Trim().Trim('/').ToLowerInvariant()));
        }

        var categories = await _categories.ListAsync(cancellationToken);
        foreach (var category in categories.Where(c => c.IsActive))
        {
            if (string.IsNullOrWhiteSpace(category.Slug))
                continue;
            list.Add(new CommentTargetCandidate(
                CommentTargetType.ProductCategory,
                category.Id,
                category.Name,
                "/shop/category/" + category.Slug.Trim().Trim('/').ToLowerInvariant()));
        }

        return list;
    }

    public async Task<CommentTargetCandidate?> GetAsync(
        CommentTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        return targetType switch
        {
            CommentTargetType.BlogPost => await GetBlogAsync(targetId, cancellationToken),
            CommentTargetType.Event => await GetEventAsync(targetId, cancellationToken),
            CommentTargetType.Product => await GetProductAsync(targetId, cancellationToken),
            CommentTargetType.ProductCategory => await GetCategoryAsync(targetId, cancellationToken),
            _ => null
        };
    }

    private async Task<CommentTargetCandidate?> GetBlogAsync(Guid id, CancellationToken cancellationToken)
    {
        var post = await _posts.GetAsync(id, cancellationToken);
        if (post is null || string.IsNullOrWhiteSpace(post.Slug))
            return null;
        return new CommentTargetCandidate(
            CommentTargetType.BlogPost,
            post.Id,
            post.Title,
            "/" + post.Slug.Trim().Trim('/').ToLowerInvariant());
    }

    private async Task<CommentTargetCandidate?> GetEventAsync(Guid id, CancellationToken cancellationToken)
    {
        var article = await _articles.GetAsync(id, cancellationToken);
        if (article is null || article.Kind != ArticleKind.Event || string.IsNullOrWhiteSpace(article.Slug))
            return null;
        return new CommentTargetCandidate(
            CommentTargetType.Event,
            article.Id,
            article.Title,
            "/event/" + article.Slug.Trim().Trim('/').ToLowerInvariant());
    }

    private async Task<CommentTargetCandidate?> GetProductAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _products.GetAsync(id, cancellationToken);
        if (product is null || string.IsNullOrWhiteSpace(product.Slug))
            return null;
        return new CommentTargetCandidate(
            CommentTargetType.Product,
            product.Id,
            product.Title,
            "/shop/" + product.Slug.Trim().Trim('/').ToLowerInvariant());
    }

    private async Task<CommentTargetCandidate?> GetCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var category = await _categories.GetAsync(id, cancellationToken);
        if (category is null || string.IsNullOrWhiteSpace(category.Slug))
            return null;
        return new CommentTargetCandidate(
            CommentTargetType.ProductCategory,
            category.Id,
            category.Name,
            "/shop/category/" + category.Slug.Trim().Trim('/').ToLowerInvariant());
    }
}
