using CMS.Application.Seo;
using CMS.Modules.Blog.Domain.Enums;
using CMS.Modules.Blog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Blog.Infrastructure.Seo;

public sealed class BlogInternalLinkCandidateProvider : IInternalLinkCandidateProvider
{
    private readonly BlogDbContext _db;

    public BlogInternalLinkCandidateProvider(BlogDbContext db)
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

        var posts = await _db.Posts.AsNoTracking()
            .Where(p => p.Status == PostStatus.Published
                        && (p.Title.Contains(term) || p.Slug.Contains(term)))
            .OrderByDescending(p => p.PublishedAtUtc)
            .Take(take)
            .Select(p => new { p.Id, p.Title, p.Slug })
            .ToListAsync(cancellationToken);

        return posts
            .Select(p => new InternalLinkCandidate(p.Title, $"/{p.Slug}", SeoContentTypeKeys.BlogPost, p.Id))
            .ToList();
    }
}
