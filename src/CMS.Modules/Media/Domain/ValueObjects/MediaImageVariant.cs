namespace CMS.Modules.Media.Domain.ValueObjects;

public sealed record MediaImageVariant(
    string Name,
    string ObjectKey,
    int Width,
    int Height,
    long SizeBytes,
    string ContentType);
