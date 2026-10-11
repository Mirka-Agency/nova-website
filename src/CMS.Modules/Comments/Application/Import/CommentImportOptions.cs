namespace CMS.Modules.Comments.Application.Import;

public sealed class CommentImportOptions
{
    public const string SectionName = "Comments:Import";

    /// <summary>Legacy WordPress hosts that should be treated as equivalent to the current site.</summary>
    public List<string> LegacyHosts { get; set; } = [];

    /// <summary>Current site hosts accepted when matching absolute URLs.</summary>
    public List<string> CurrentHosts { get; set; } = [];

    /// <summary>Maximum upload size in bytes (default 10 MB).</summary>
    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Preview session lifetime.</summary>
    public int PreviewTtlMinutes { get; set; } = 60;

    /// <summary>Imports above this count are processed via the background queue.</summary>
    public int BackgroundThreshold { get; set; } = 300;

    /// <summary>Batch size for DB writes.</summary>
    public int BatchSize { get; set; } = 100;
}
