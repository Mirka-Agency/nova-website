using System.Text.Json;

namespace CMS.Modules.Video.Web.Upload;

/// <summary>Temp-disk sessions for chunked admin video uploads.</summary>
internal static class VideoChunkUploadStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string RootPath => Path.Combine(Path.GetTempPath(), "cms-video-chunks");

    public static VideoChunkSession Create(
        string fileName,
        string contentType,
        long totalBytes,
        int totalChunks)
    {
        var uploadId = Guid.NewGuid().ToString("N");
        var dir = GetDirectory(uploadId);
        Directory.CreateDirectory(dir);

        var session = new VideoChunkSession
        {
            UploadId = uploadId,
            FileName = fileName,
            ContentType = contentType,
            TotalBytes = totalBytes,
            TotalChunks = totalChunks,
            ReceivedChunks = [],
            CreatedAtUtc = DateTime.UtcNow
        };
        Save(session);
        return session;
    }

    public static VideoChunkSession? TryGet(string uploadId)
    {
        if (!IsValidUploadId(uploadId))
            return null;

        var path = GetMetaPath(uploadId);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<VideoChunkSession>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static void Save(VideoChunkSession session)
    {
        Directory.CreateDirectory(GetDirectory(session.UploadId));
        File.WriteAllText(GetMetaPath(session.UploadId), JsonSerializer.Serialize(session, JsonOptions));
    }

    public static string GetChunkPath(string uploadId, int index) =>
        Path.Combine(GetDirectory(uploadId), $"part-{index:D5}");

    public static void Delete(string uploadId)
    {
        if (!IsValidUploadId(uploadId))
            return;

        var dir = GetDirectory(uploadId);
        if (!Directory.Exists(dir))
            return;

        try
        {
            Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    public static bool IsValidUploadId(string? uploadId) =>
        !string.IsNullOrWhiteSpace(uploadId)
        && uploadId.Length == 32
        && uploadId.All(c => char.IsAsciiHexDigit(c));

    private static string GetDirectory(string uploadId) => Path.Combine(RootPath, uploadId);

    private static string GetMetaPath(string uploadId) => Path.Combine(GetDirectory(uploadId), "meta.json");
}

internal sealed class VideoChunkSession
{
    public string UploadId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public int TotalChunks { get; set; }
    public HashSet<int> ReceivedChunks { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
}
