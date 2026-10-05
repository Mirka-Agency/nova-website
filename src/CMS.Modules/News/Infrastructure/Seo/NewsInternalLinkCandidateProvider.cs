using CMS.Application.Seo;
using CMS.Modules.News.Domain.Enums;
using CMS.Modules.News.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.News.Infrastructure.Seo;

public sealed class NewsInternalLinkCandidateProvider : IInternalLinkCandidateProvider
{
    private readonly NewsDbContext _db;

    public NewsInternalLinkCandidateProvider(NewsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<InternalLinkCandidate>> SearchAsync(
        string query,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        take = Math.Clamp(take, 1, 50);
        var term = query.Trim();

        var articles = await _db.Articles.AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published
                        && (a.Title.Contains(term) || a.Slug.Contains(term)))
            .OrderByDescending(a => a.PublishedAtUtc)
            .Take(take)
            .Select(a => new { a.Id, a.Title, a.Slug })
            .ToListAsync(cancellationToken);

        return articles
            .Select(a => new InternalLinkCandidate(a.Title, $"/education-articles/{a.Slug}", SeoContentTypeKeys.NewsArticle, a.Id))
            .ToList();
    }
}
