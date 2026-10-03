using CMS.Application.Seo;
using CMS.Modules.Seo.Application.Tools;

namespace CMS.Modules.Seo.Application.Interfaces;

public interface ISeoLinkSuggestionService
{
    Task<IReadOnlyList<InternalLinkCandidate>> SuggestAsync(
        string keyword,
        string? excludeContentType,
        Guid? excludeId,
        int take = 10,
        CancellationToken cancellationToken = default);
}

public interface IBrokenLinkChecker
{
    Task<IReadOnlyList<BrokenLinkCheckResult>> CheckAsync(
        IEnumerable<string> urls,
        CancellationToken cancellationToken = default);
}
