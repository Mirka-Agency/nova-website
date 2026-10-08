using System.Globalization;
using System.Net;
using System.Text;

namespace CMS.Application.Seo;

public static class SitemapXmlBuilder
{
    public static string Build(string baseUrl, IReadOnlyList<SitemapUrlEntry> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentNullException.ThrowIfNull(entries);

        var root = baseUrl.TrimEnd('/');
        var sb = new StringBuilder(capacity: 256 + entries.Count * 160);
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.RelativePath))
                continue;

            var path = EnsureTrailingSlashPath(entry.RelativePath.Trim());
            var loc = root + path;
            if (!seen.Add(loc))
                continue;

            sb.AppendLine("  <url>");
            sb.Append("    <loc>").Append(XmlEscape(loc)).AppendLine("</loc>");

            if (entry.LastModifiedUtc is { } lastMod)
            {
                var utc = lastMod.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(lastMod, DateTimeKind.Utc)
                    : lastMod.ToUniversalTime();
                sb.Append("    <lastmod>")
                    .Append(utc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))
                    .AppendLine("</lastmod>");
            }

            if (entry.ChangeFrequency is { } freq)
            {
                sb.Append("    <changefreq>")
                    .Append(ToChangefreq(freq))
                    .AppendLine("</changefreq>");
            }

            if (entry.Priority is { } priority)
            {
                var clamped = Math.Clamp(priority, 0d, 1d);
                sb.Append("    <priority>")
                    .Append(clamped.ToString("0.0", CultureInfo.InvariantCulture))
                    .AppendLine("</priority>");
            }

            sb.AppendLine("  </url>");
        }

        sb.Append("</urlset>");
        return sb.ToString();
    }

    private static string ToChangefreq(SitemapChangeFrequency frequency) => frequency switch
    {
        SitemapChangeFrequency.Always => "always",
        SitemapChangeFrequency.Hourly => "hourly",
        SitemapChangeFrequency.Daily => "daily",
        SitemapChangeFrequency.Weekly => "weekly",
        SitemapChangeFrequency.Monthly => "monthly",
        SitemapChangeFrequency.Yearly => "yearly",
        SitemapChangeFrequency.Never => "never",
        _ => "weekly"
    };

    private static string EnsureTrailingSlashPath(string path)
    {
        if (!path.StartsWith('/'))
            path = "/" + path;

        if (path.Length > 1 && !path.EndsWith('/'))
        {
            var query = path.IndexOf('?', StringComparison.Ordinal);
            if (query < 0)
                path += "/";
            else if (query > 0 && path[query - 1] != '/')
                path = path[..query] + "/" + path[query..];
        }

        return path;
    }

    private static string XmlEscape(string value) =>
        WebUtility.HtmlEncode(value);
}
