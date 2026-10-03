using System.Text;
using System.Text.RegularExpressions;

namespace CMS.Application.Storage;

/// <summary>
/// Builds stable object keys: <c>{module}/{category}/{guid}_{safeFileName}</c>.
/// </summary>
public static partial class ObjectStorageKeys
{
    public static class Modules
    {
        public const string Blog = "blog";
        public const string News = "news";
        public const string Services = "services";
        public const string Video = "video";
        public const string Team = "team";
        public const string Honors = "honors";
        public const string Voices = "voices";
        public const string Shop = "shop";
        public const string Forms = "forms";
        public const string Media = "media";
    }

    public static string Create(string module, string category, string originalFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        var safeName = SanitizeFileName(originalFileName);
        return $"{module.Trim().ToLowerInvariant()}/{category.Trim().ToLowerInvariant()}/{Guid.NewGuid():N}_{safeName}";
    }

    private static string SanitizeFileName(string? originalFileName)
    {
        var name = Path.GetFileName(originalFileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(name))
            return "file";

        var extension = Path.GetExtension(name);
        var baseName = Path.GetFileNameWithoutExtension(name);

        var normalized = InvalidFileNameChars().Replace(baseName, "-");
        normalized = CollapseDashes().Replace(normalized, "-").Trim('-');
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "file";

        if (normalized.Length > 80)
            normalized = normalized[..80];

        var sb = new StringBuilder(normalized.Length + extension.Length);
        sb.Append(normalized.ToLowerInvariant());
        if (!string.IsNullOrEmpty(extension) && extension.Length <= 16)
            sb.Append(extension.ToLowerInvariant());

        return sb.ToString();
    }

    [GeneratedRegex(@"[^a-zA-Z0-9._-]+", RegexOptions.CultureInvariant)]
    private static partial Regex InvalidFileNameChars();

    [GeneratedRegex(@"-+", RegexOptions.CultureInvariant)]
    private static partial Regex CollapseDashes();
}
