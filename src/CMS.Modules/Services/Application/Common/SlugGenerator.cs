using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CMS.Modules.Services.Application.Common;

public static partial class SlugGenerator
{
    public static string FromTitle(string title, int maxLength = 300)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var normalized = title.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else if (char.IsWhiteSpace(ch) || ch is '-' or '_' or '‌')
            {
                sb.Append('-');
            }
        }

        var slug = CollapseDashes().Replace(sb.ToString(), "-").Trim('-');
        return slug.Length > maxLength ? slug[..maxLength].TrimEnd('-') : slug;
    }

    /// <summary>
    /// Yoast-style heading anchor: h-{persian-slug}, e.g. h-تفاوت-نوع-ریزش-مو-در-کم-کاری-و-پرکاری
    /// Does not NFD-strip Arabic letters (keeps ئ etc.) so anchors match nova-clinic.org.
    /// </summary>
    public static string HeadingIdFromTitle(string title, int maxLength = 300)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var slugMax = Math.Max(1, maxLength - 2);
        var sb = new StringBuilder(title.Length);

        foreach (var ch in title.Trim())
        {
            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToLowerInvariant(ch));
            else if (char.IsWhiteSpace(ch) || ch is '-' or '_' or '‌')
                sb.Append('-');
        }

        var slug = CollapseDashes().Replace(sb.ToString(), "-").Trim('-');
        if (string.IsNullOrEmpty(slug))
            return string.Empty;

        if (slug.Length > slugMax)
            slug = slug[..slugMax].TrimEnd('-');

        return $"h-{slug}";
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex CollapseDashes();
}
