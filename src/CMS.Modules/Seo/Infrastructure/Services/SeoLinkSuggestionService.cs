using CMS.Application.Seo;
using CMS.Modules.Seo.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Seo.Infrastructure.Services;

public sealed class SeoLinkSuggestionService : ISeoLinkSuggestionService
{
    private readonly IEnumerable<IInternalLinkCandidateProvider> _providers;

    public SeoLinkSuggestionService(IEnumerable<IInternalLinkCandidateProvider> providers)
    {
        _providers = providers;
    }

    public async Task<IReadOnlyList<InternalLinkCandidate>> SuggestAsync(
        string keyword,
        string? excludeContentType,
        Guid? excludeId,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return [];

        take = Math.Clamp(take, 1, 50);
        var query = keyword.Trim();
        var results = new List<InternalLinkCandidate>();

        foreach (var provider in _providers)
        {
            var batch = await provider.SearchAsync(query, take, cancellationToken);
            results.AddRange(batch);
        }

        IEnumerable<InternalLinkCandidate> filtered = results;
        if (!string.IsNullOrWhiteSpace(excludeContentType) && excludeId.HasValue)
        {
            var type = excludeContentType.Trim().ToLowerInvariant();
            filtered = filtered.Where(c =>
                !(c.ContentType.Equals(type, StringComparison.OrdinalIgnoreCase) && c.ContentId == excludeId.Value));
        }

        return filtered
            .GroupBy(c => (c.ContentType, c.ContentId))
            .Select(g => g.First())
            .Take(take)
            .ToList();
    }
}
