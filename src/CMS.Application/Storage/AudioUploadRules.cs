namespace CMS.Application.Storage;

/// <summary>Upload guards for patient voice / audio testimonials.</summary>
public static class AudioUploadRules
{
    public const long MaxBytes = 20 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/mpeg",
        "audio/mp3",
        "audio/mp4",
        "audio/aac",
        "audio/ogg",
        "audio/wav",
        "audio/x-wav",
        "audio/webm",
        "audio/x-m4a"
    };

    public static bool IsAllowedContentType(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) && AllowedContentTypes.Contains(contentType.Trim());

    public static bool IsWithinSizeLimit(long length) => length > 0 && length <= MaxBytes;

    public static bool Validate(Stream stream, string? contentType, long length)
    {
        if (!IsAllowedContentType(contentType) || !IsWithinSizeLimit(length))
            return false;

        return FileContentSniffer.MatchesDeclaredAudio(stream, contentType);
    }
}
