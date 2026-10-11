namespace CMS.Modules.Comments.Application.Import;

public sealed class CommentUrlMatcher : ICommentUrlMatcher
{
    public string? NormalizePath(string? pageUrl, IReadOnlyCollection<string> trustedHosts)
    {
        if (string.IsNullOrWhiteSpace(pageUrl))
            return null;

        var raw = pageUrl.Trim();
        if (!TryCreateUri(raw, out var uri))
            return null;

        var path = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString.Split('?', '#')[0];
        path = Uri.UnescapeDataString(path);
        path = path.Replace('\\', '/');

        if (!path.StartsWith('/'))
            path = "/" + path;

        while (path.Contains("//", StringComparison.Ordinal))
            path = path.Replace("//", "/", StringComparison.Ordinal);

        if (path.Length > 1)
            path = path.TrimEnd('/');

        if (path.EndsWith("/index.html", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/index.htm", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/index.php", StringComparison.OrdinalIgnoreCase))
        {
            path = path[..path.LastIndexOf('/')];
            if (string.IsNullOrEmpty(path))
                path = "/";
        }

        return path.ToLowerInvariant();
    }

    public CommentUrlMatchResult Match(
        string? pageUrl,
        IReadOnlyList<CommentTargetCandidate> catalog,
        IReadOnlyCollection<string> trustedHosts)
    {
        var path = NormalizePath(pageUrl, trustedHosts);
        if (string.IsNullOrWhiteSpace(path) || path == "/")
            return new CommentUrlMatchResult(CommentUrlMatchKind.None, null, []);

        var candidates = new List<CommentTargetCandidate>();
        foreach (var item in catalog)
        {
            var candidatePath = NormalizePath(item.CanonicalPath, trustedHosts);
            if (string.IsNullOrWhiteSpace(candidatePath))
                continue;

            if (string.Equals(candidatePath, path, StringComparison.Ordinal)
                || AreEquivalentContentPaths(path, candidatePath))
            {
                candidates.Add(item);
            }
        }

        if (candidates.Count == 0)
            candidates.AddRange(MatchBySlugFallback(path, catalog, trustedHosts));

        candidates = candidates
            .GroupBy(c => (c.TargetType, c.TargetId))
            .Select(g => g.First())
            .ToList();

        if (candidates.Count == 0)
            return new CommentUrlMatchResult(CommentUrlMatchKind.None, null, []);

        if (candidates.Count > 1)
            return new CommentUrlMatchResult(CommentUrlMatchKind.Ambiguous, null, candidates);

        return new CommentUrlMatchResult(CommentUrlMatchKind.Exact, candidates[0], candidates);
    }

    /// <summary>
    /// Matches WordPress-style permalinks by last slug segment when full path differs
    /// (e.g. /2023/05/10/my-post/, /category/x/my-post/, /fa/my-post/).
    /// </summary>
    private static IEnumerable<CommentTargetCandidate> MatchBySlugFallback(
        string path,
        IReadOnlyList<CommentTargetCandidate> catalog,
        IReadOnlyCollection<string> trustedHosts)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            yield break;

        var slug = segments[^1];
        if (string.IsNullOrWhiteSpace(slug) || slug is "blog" or "shop" or "event" or "events" or "news")
            yield break;

        // /shop/category/{slug}
        if (segments.Length >= 3
            && string.Equals(segments[0], "shop", StringComparison.Ordinal)
            && string.Equals(segments[1], "category", StringComparison.Ordinal))
        {
            foreach (var item in catalog.Where(c =>
                         c.TargetType == Domain.Enums.CommentTargetType.ProductCategory
                         && SlugOf(c.CanonicalPath, trustedHosts) == slug))
            {
                yield return item;
            }

            yield break;
        }

        // /shop/{slug}
        if (segments.Length >= 2 && string.Equals(segments[0], "shop", StringComparison.Ordinal))
        {
            foreach (var item in catalog.Where(c =>
                         c.TargetType == Domain.Enums.CommentTargetType.Product
                         && SlugOf(c.CanonicalPath, trustedHosts) == slug))
            {
                yield return item;
            }

            yield break;
        }

        // /event|events/{slug}
        if (segments.Length >= 2
            && (string.Equals(segments[0], "event", StringComparison.Ordinal)
                || string.Equals(segments[0], "events", StringComparison.Ordinal)))
        {
            foreach (var item in catalog.Where(c =>
                         c.TargetType == Domain.Enums.CommentTargetType.Event
                         && SlugOf(c.CanonicalPath, trustedHosts) == slug))
            {
                yield return item;
            }

            yield break;
        }

        // Blog: root slug, /blog/{slug}, date/category permalinks → last segment
        foreach (var item in catalog.Where(c =>
                     c.TargetType == Domain.Enums.CommentTargetType.BlogPost
                     && SlugOf(c.CanonicalPath, trustedHosts) == slug))
        {
            yield return item;
        }
    }

    private static string? SlugOf(string canonicalPath, IReadOnlyCollection<string> trustedHosts)
    {
        var matcher = new CommentUrlMatcher();
        var path = matcher.NormalizePath(canonicalPath, trustedHosts);
        if (string.IsNullOrWhiteSpace(path))
            return null;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0 ? null : segments[^1];
    }

    internal static bool AreEquivalentContentPaths(string left, string right)
    {
        var leftSlug = ExtractComparableKey(left);
        var rightSlug = ExtractComparableKey(right);
        return leftSlug is not null
               && rightSlug is not null
               && string.Equals(leftSlug, rightSlug, StringComparison.Ordinal);
    }

    private static string? ExtractComparableKey(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return null;

        if (segments.Length == 1)
            return "blog:" + segments[0];

        if (segments.Length == 2
            && string.Equals(segments[0], "blog", StringComparison.Ordinal))
            return "blog:" + segments[1];

        if (segments.Length == 2
            && (string.Equals(segments[0], "event", StringComparison.Ordinal)
                || string.Equals(segments[0], "events", StringComparison.Ordinal)))
            return "event:" + segments[1];

        if (segments.Length == 2
            && (string.Equals(segments[0], "education-articles", StringComparison.Ordinal)
                || string.Equals(segments[0], "news", StringComparison.Ordinal)))
            return "news:" + segments[1];

        if (segments.Length == 2
            && string.Equals(segments[0], "shop", StringComparison.Ordinal))
            return "product:" + segments[1];

        if (segments.Length == 3
            && string.Equals(segments[0], "shop", StringComparison.Ordinal)
            && string.Equals(segments[1], "category", StringComparison.Ordinal))
            return "product-category:" + segments[2];

        // /yyyy/mm/dd/slug or /yyyy/mm/slug
        if (segments.Length >= 2 && LooksLikeYear(segments[0]))
            return "blog:" + segments[^1];

        // /category/.../slug or /tag/.../slug or language prefix /fa/slug
        if (segments.Length >= 2)
            return "blog:" + segments[^1];

        return string.Join('/', segments);
    }

    private static bool LooksLikeYear(string segment) =>
        segment.Length == 4 && int.TryParse(segment, out var year) && year is >= 1990 and <= 2100;

    private static bool TryCreateUri(string raw, out Uri uri)
    {
        if (Uri.TryCreate(raw, UriKind.Absolute, out var absolute))
        {
            uri = absolute;
            return true;
        }

        if (raw.StartsWith('/'))
        {
            if (Uri.TryCreate("https://placeholder.local" + raw, UriKind.Absolute, out var relative))
            {
                uri = relative;
                return true;
            }
        }
        else if (!raw.Contains("://", StringComparison.Ordinal)
                 && raw.Contains('.', StringComparison.Ordinal)
                 && !raw.Contains(' ', StringComparison.Ordinal))
        {
            if (Uri.TryCreate("https://" + raw, UriKind.Absolute, out var hostPath))
            {
                uri = hostPath;
                return true;
            }
        }
        else if (Uri.TryCreate("https://placeholder.local/" + raw.TrimStart('/'), UriKind.Absolute, out var fallback))
        {
            uri = fallback;
            return true;
        }

        uri = null!;
        return false;
    }
}
