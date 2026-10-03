namespace CMS.Application.Storage;

/// <summary>Upload guards for form file fields (resume, attachments).</summary>
public static class FormFileUploadRules
{
    public const long MaxBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "text/plain"
    };

    public static bool IsAllowedContentType(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) && AllowedContentTypes.Contains(contentType.Trim());

    public static bool IsWithinSizeLimit(long length) => length > 0 && length <= MaxBytes;

    public static bool Validate(Stream stream, string? contentType, long length)
    {
        if (!IsAllowedContentType(contentType) || !IsWithinSizeLimit(length))
            return false;

        return FileContentSniffer.MatchesDeclaredFormFile(stream, contentType);
    }
}
