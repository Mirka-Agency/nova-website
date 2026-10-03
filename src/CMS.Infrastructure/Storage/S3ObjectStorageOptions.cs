namespace CMS.Infrastructure.Storage;

public sealed class S3ObjectStorageOptions
{
    public const string SectionName = "Storage:S3";

    /// <summary>Access key id (or compatible provider key).</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>Secret access key.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Target bucket name.</summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>AWS region (ignored by some S3-compatible providers when ServiceUrl is set).</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Optional custom endpoint for MinIO / Arvan / Liara / other S3-compatible APIs.
    /// Example: <c>http://localhost:9000</c> or <c>https://s3.ir-thr-at1.arvanstorage.ir</c>.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Optional public base URL (CDN or public bucket host) used when building object URLs.
    /// Example: <c>https://cdn.example.com</c> or <c>https://my-bucket.s3.amazonaws.com</c>.
    /// When empty, a URL is derived from ServiceUrl or the standard AWS virtual-host style.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// Use path-style addressing (<c>endpoint/bucket/key</c>). Required for many MinIO setups.
    /// </summary>
    public bool ForcePathStyle { get; set; }

    /// <summary>
    /// When true, uploads set ACL <c>public-read</c>. Prefer bucket/CDN policy instead when ACLs are disabled.
    /// </summary>
    public bool UsePublicReadAcl { get; set; }
}
