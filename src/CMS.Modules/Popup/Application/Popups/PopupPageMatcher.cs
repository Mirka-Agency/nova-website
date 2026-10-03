namespace CMS.Modules.Popup.Application.Popups;

/// <summary>Matches request paths against include/exclude rules (supports trailing * wildcards).</summary>
public static class PopupPageMatcher
{
    public static bool IsHomepage(string path)
    {
        var normalized = NormalizePath(path);
        return normalized is "/" or "";
    }

    public static bool MatchesAny(string path, IEnumerable<string> patterns)
    {
        var normalized = NormalizePath(path);
        foreach (var pattern in patterns)
        {
            if (Matches(normalized, pattern))
                return true;
        }

        return false;
    }

    public static IReadOnlyList<string> ParsePaths(string? pagePaths)
    {
        if (string.IsNullOrWhiteSpace(pagePaths))
            return [];

        return pagePaths
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizePath)
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        var value = path.Trim();
        if (!value.StartsWith('/'))
            value = "/" + value;

        // Strip query/hash if present
        var q = value.IndexOfAny(['?', '#']);
        if (q >= 0)
            value = value[..q];

        if (value.Length > 1 && value.EndsWith('/'))
            value = value.TrimEnd('/');

        return string.IsNullOrEmpty(value) ? "/" : value;
    }

    private static bool Matches(string path, string pattern)
    {
        var p = NormalizePath(pattern);
        if (p.EndsWith("/*", StringComparison.Ordinal))
        {
            var prefix = p[..^1]; // keep trailing slash intent: /blog/*
            if (prefix.EndsWith('/'))
                return path.Equals(prefix.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)
                       || path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        if (p.EndsWith('*'))
        {
            var prefix = p.TrimEnd('*');
            return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return path.Equals(p, StringComparison.OrdinalIgnoreCase);
    }
}
