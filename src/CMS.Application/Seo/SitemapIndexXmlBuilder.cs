using System.Net;
using System.Text;

namespace CMS.Application.Seo;

public static class SitemapIndexXmlBuilder
{
    public static string Build(string baseUrl, IReadOnlyList<string> segmentRelativePaths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentNullException.ThrowIfNull(segmentRelativePaths);

        var root = baseUrl.TrimEnd('/');
        var sb = new StringBuilder(capacity: 256 + segmentRelativePaths.Count * 120);
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

        foreach (var relativePath in segmentRelativePaths)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                continue;

            var path = relativePath.Trim();
            if (!path.StartsWith('/'))
                path = "/" + path;

            sb.AppendLine("  <sitemap>");
            sb.Append("    <loc>").Append(XmlEscape(root + path)).AppendLine("</loc>");
            sb.AppendLine("  </sitemap>");
        }

        sb.Append("</sitemapindex>");
        return sb.ToString();
    }

    private static string XmlEscape(string value) =>
        WebUtility.HtmlEncode(value);
}
