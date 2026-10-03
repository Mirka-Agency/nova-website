namespace CMS.Application.Storage;

/// <summary>Shared upload guards for image files across modules.</summary>
public static class ImageUploadRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    public static bool IsAllowedContentType(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) && AllowedContentTypes.Contains(contentType.Trim());

    public static bool IsWithinSizeLimit(long length) => length > 0 && length <= MaxBytes;

    public static bool Validate(Stream stream, string? contentType, long length)
    {
        if (!IsAllowedContentType(contentType) || !IsWithinSizeLimit(length))
            return false;

        return FileContentSniffer.MatchesDeclaredImage(stream, contentType);
    }
}
