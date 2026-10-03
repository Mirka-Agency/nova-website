namespace CMS.Modules.Media.Domain.ValueObjects;

public sealed record MediaFileRef(
    string FileName,
    string ContentType,
    long SizeBytes,
    string ObjectKey,
    int? Width = null,
    int? Height = null);
