namespace CMS.Application.Storage;

/// <summary>Shared upload guards for video files (Video module).</summary>
public static class VideoUploadRules
{
    public const long MaxBytes = 200 * 1024 * 1024;

    /// <summary>Files larger than this are uploaded in chunks from the browser.</summary>
    public const long ChunkThresholdBytes = 5 * 1024 * 1024;

    /// <summary>Preferred chunk size for browser → server uploads.</summary>
    public const long ChunkSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4",
        "video/webm",
        "video/ogg",
        "video/quicktime"
    };

    public static bool IsAllowedContentType(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) && AllowedContentTypes.Contains(contentType.Trim());

    public static bool IsWithinSizeLimit(long length) => length > 0 && length <= MaxBytes;

    public static int GetChunkCount(long totalBytes)
    {
        if (totalBytes <= 0)
            return 0;

        return (int)((totalBytes + ChunkSizeBytes - 1) / ChunkSizeBytes);
    }

    public static bool Validate(Stream stream, string? contentType, long length)
    {
        if (!IsAllowedContentType(contentType) || !IsWithinSizeLimit(length))
            return false;

        return FileContentSniffer.MatchesDeclaredVideo(stream, contentType);
    }
}
