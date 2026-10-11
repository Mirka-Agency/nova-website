namespace CMS.Application.Storage;

/// <summary>Result of applying Cache-Control metadata to existing objects.</summary>
public sealed record ObjectStorageCacheBackfillResult(
    int Examined,
    int Updated,
    int Skipped,
    int Failed);
