namespace CMS.Application.Storage;

/// <summary>
/// Central browser-cache policy for S3 / S3-compatible object URLs.
/// Public CMS media uses immutable GUID keys, so long-lived Cache-Control is safe.
/// Form uploads and unknown prefixes stay off the long public cache.
/// </summary>
public static class ObjectStorageCachePolicy
{
    /// <summary>One year; safe when object keys are content-addressed / GUID-unique.</summary>
    public const string PublicImmutable = "public, max-age=31536000, immutable";

    /// <summary>Do not park sensitive form attachments in shared browser/CDN caches.</summary>
    public const string PrivateNoStore = "private, no-store";

    private static readonly HashSet<string> PublicModules = new(StringComparer.OrdinalIgnoreCase)
    {
        ObjectStorageKeys.Modules.Blog,
        ObjectStorageKeys.Modules.News,
        ObjectStorageKeys.Modules.Services,
        ObjectStorageKeys.Modules.Video,
        ObjectStorageKeys.Modules.Team,
        ObjectStorageKeys.Modules.Honors,
        ObjectStorageKeys.Modules.Voices,
        ObjectStorageKeys.Modules.Shop,
        ObjectStorageKeys.Modules.Media,
    };

    /// <summary>
    /// Resolves the Cache-Control value to store on the object, or <c>null</c> to leave unset.
    /// </summary>
    public static string? ResolveCacheControl(string objectKey)
    {
        var module = GetModulePrefix(objectKey);
        if (module is null)
            return null;

        if (string.Equals(module, ObjectStorageKeys.Modules.Forms, StringComparison.OrdinalIgnoreCase))
            return PrivateNoStore;

        if (PublicModules.Contains(module))
            return PublicImmutable;

        return null;
    }

    /// <summary>True when the object should receive the long public immutable policy.</summary>
    public static bool IsPublicImmutableCandidate(string objectKey) =>
        string.Equals(ResolveCacheControl(objectKey), PublicImmutable, StringComparison.Ordinal);

    /// <summary>
    /// True when existing object metadata already matches the desired Cache-Control value.
    /// </summary>
    public static bool HasDesiredCacheControl(string objectKey, string? existingCacheControl)
    {
        var desired = ResolveCacheControl(objectKey);
        if (desired is null)
            return true;

        if (string.IsNullOrWhiteSpace(existingCacheControl))
            return false;

        return Normalize(existingCacheControl) == Normalize(desired);
    }

    private static string? GetModulePrefix(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return null;

        var key = objectKey.Trim().TrimStart('/');
        var slash = key.IndexOf('/');
        var module = slash < 0 ? key : key[..slash];
        return string.IsNullOrWhiteSpace(module) ? null : module;
    }

    private static string Normalize(string value) =>
        string.Join(',', value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Replace(" ", "", StringComparison.Ordinal).ToLowerInvariant()));
}
