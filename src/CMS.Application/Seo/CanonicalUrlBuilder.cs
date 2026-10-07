namespace CMS.Application.Seo;

/// <summary>
/// Builds a single absolute canonical URL: lowercase path, no trailing slash (except /),
/// legacy public-path rewrites, and only <c>page</c> query when &gt; 1.
/// </summary>
public static class CanonicalUrlBuilder
{
    public static string Build(
        string? configured,
        string scheme,
        string host,
        string? pathBase = null,
        string? path = null,
        string? pageQueryValue = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheme);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        var requestBase = $"{scheme}://{host}".TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(configured))
        {
            var value = configured.Trim();
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                var sameHost = IsSameHost(uri, host);
                var absPath = NormalizePath(uri.AbsolutePath, applyLegacyRewrites: sameHost);
                var page = ParsePage(uri.Query);
                if (sameHost)
                    return Compose(requestBase, absPath, page);

                var authority = $"{uri.Scheme}://{uri.Authority}".TrimEnd('/');
                return Compose(authority, absPath, page);
            }

            var relative = value.StartsWith('/') ? value : "/" + value;
            var q = relative.IndexOf('?', StringComparison.Ordinal);
            string pathPart;
            int? pageFromRelative = null;
            if (q >= 0)
            {
                pageFromRelative = ParsePage(relative[q..]);
                pathPart = relative[..q];
            }
            else
            {
                pathPart = relative;
            }

            return Compose(requestBase, NormalizePath(pathPart, applyLegacyRewrites: true), pageFromRelative);
        }

        var requestPath = $"{pathBase}{path}";
        if (string.IsNullOrEmpty(requestPath))
            requestPath = "/";

        int? pageFromRequest = null;
        if (int.TryParse(pageQueryValue, out var pageNum) && pageNum > 1)
            pageFromRequest = pageNum;

        return Compose(requestBase, NormalizePath(requestPath, applyLegacyRewrites: true), pageFromRequest);
    }

    /// <summary>
    /// Absolute URL for a content item: uses custom canonical when set, otherwise <paramref name="fallbackRelativePath"/>.
    /// </summary>
    public static string ForContent(string? configuredCanonical, string fallbackRelativePath, string scheme, string host)
    {
        var value = string.IsNullOrWhiteSpace(configuredCanonical)
            ? fallbackRelativePath
            : configuredCanonical;
        return Build(value, scheme, host);
    }

    private static string NormalizePath(string path, bool applyLegacyRewrites)
    {
        if (string.IsNullOrEmpty(path))
            return "/";

        path = path.ToLowerInvariant();

        if (applyLegacyRewrites)
        {
            if (path == "/news" || path.StartsWith("/news/", StringComparison.Ordinal))
                path = "/education-articles" + path["/news".Length..];
            else if (path == "/teams" || path.StartsWith("/teams/", StringComparison.Ordinal))
                path = "/doctors" + path["/teams".Length..];
            else if (path == "/events" || path.StartsWith("/events/", StringComparison.Ordinal))
                path = "/event" + path["/events".Length..];
            else if (path == "/about")
                path = "/about-us";
            else if (path == "/contact")
                path = "/contact-us";
        }

        if (path.Length > 1 && path.EndsWith('/'))
            path = path.TrimEnd('/');

        return path;
    }

    private static int? ParsePage(string query)
    {
        if (string.IsNullOrEmpty(query))
            return null;

        var q = query.StartsWith('?') ? query[1..] : query;
        foreach (var part in q.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            var key = eq >= 0 ? part[..eq] : part;
            if (!key.Equals("page", StringComparison.OrdinalIgnoreCase))
                continue;
            var raw = eq >= 0 ? part[(eq + 1)..] : "";
            if (int.TryParse(raw, out var page) && page > 1)
                return page;
            return null;
        }

        return null;
    }

    private static bool IsSameHost(Uri uri, string hostHeader)
    {
        var hostString = hostHeader;
        var port = -1;
        var colon = hostHeader.LastIndexOf(':');
        if (colon > 0
            && hostHeader.IndexOf(']') < colon
            && int.TryParse(hostHeader[(colon + 1)..], out var parsedPort))
        {
            hostString = hostHeader[..colon];
            port = parsedPort;
        }

        if (!uri.Host.Equals(hostString, StringComparison.OrdinalIgnoreCase))
            return false;

        if (port < 0)
            return true;

        var uriPort = uri.IsDefaultPort
            ? (uri.Scheme == Uri.UriSchemeHttps ? 443 : 80)
            : uri.Port;
        return uriPort == port;
    }

    private static string Compose(string baseUrl, string path, int? page) =>
        page is > 1 ? $"{baseUrl}{path}?page={page.Value}" : $"{baseUrl}{path}";
}
