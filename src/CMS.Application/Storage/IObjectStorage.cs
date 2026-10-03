namespace CMS.Application.Storage;

/// <summary>
/// Abstraction for binary object storage (uploads for Blog, Shop, Forms, etc.).
/// Implementations live in Infrastructure (S3 / S3-compatible).
/// </summary>
public interface IObjectStorage
{
    /// <summary>
    /// Uploads <paramref name="content"/> to the given object key.
    /// Returns the stored key and a publicly reachable URL.
    /// </summary>
    Task<ObjectStorageUploadResult> UploadAsync(
        Stream content,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the object if it exists. Missing keys are treated as success.
    /// </summary>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the public URL for an already-stored object key.
    /// </summary>
    string GetPublicUrl(string objectKey);
}
