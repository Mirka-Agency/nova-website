using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CMS.Modules.Blog.Application.Common;

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

    [GeneratedRegex("-{2,}")]
    private static partial Regex CollapseDashes();
}
