using CMS.Application.Seo;
using CMS.Modules.Services.Domain.Enums;
using CMS.Modules.Services.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Services.Infrastructure.Seo;

public sealed class ServicesInternalLinkCandidateProvider : IInternalLinkCandidateProvider
{
    private readonly ServicesDbContext _db;

    public ServicesInternalLinkCandidateProvider(ServicesDbContext db)
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

        var items = await _db.ServiceItems.AsNoTracking()
            .Where(p => p.Status == ServiceStatus.Published
                        && (p.Title.Contains(term) || p.Slug.Contains(term)))
            .OrderByDescending(p => p.PublishedAtUtc)
            .Take(take)
            .Select(p => new { p.Id, p.Title, p.Slug })
            .ToListAsync(cancellationToken);

        return items
            .Select(p => new InternalLinkCandidate(p.Title, $"/services/{p.Slug}", SeoContentTypeKeys.ServiceItem, p.Id))
            .ToList();
    }
}
