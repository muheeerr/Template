namespace Template.Modules.Common.Storage;

/// <summary>Configuration for S3-compatible object storage such as MinIO.</summary>
public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    public bool Enabled { get; set; }

    public string ServiceUrl { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string Region { get; set; } = "us-east-1";

    public bool ForcePathStyle { get; set; } = true;

    public bool AutoCreateBucket { get; set; } = true;

    public string DefaultBucket { get; set; } = string.Empty;

    public string ShelfCaptureBucket { get; set; } = string.Empty;

    public string ShelfCapturePrefix { get; set; } = "shelf-captures";

    public int PresignedUploadExpiryMinutes { get; set; } = 15;

    public int PresignedDownloadExpiryMinutes { get; set; } = 15;

    /// <summary>Optional HTTPS prefix (no trailing slash) for public shelf-capture objects, e.g. virtual-host bucket URL. When unset, presigned GET is used for inference.</summary>
    public string? ShelfCapturePublicHttpBaseUrl { get; set; }

    /// <summary>Presigned GET lifetime when building URLs for the external inference service.</summary>
    public int PresignedDownloadForInferenceExpiryMinutes { get; set; } = 60;
}

/// <summary>Upload request for a single object.</summary>
public sealed record ObjectStorageUploadRequest(
    string ObjectKey,
    Stream Content,
    string ContentType,
    string? Bucket = null,
    long? ContentLength = null,
    IReadOnlyDictionary<string, string>? Metadata = null);

/// <summary>Upload response for a stored object.</summary>
public sealed record ObjectStorageUploadResult(
    string Bucket,
    string ObjectKey,
    string ContentType,
    long? ContentLength,
    string? ETag);

/// <summary>Presigned upload URL request.</summary>
public sealed record ObjectStoragePresignedUploadRequest(
    string ObjectKey,
    string ContentType,
    string? Bucket = null,
    TimeSpan? ExpiresIn = null);

/// <summary>Presigned download URL request.</summary>
public sealed record ObjectStoragePresignedDownloadRequest(
    string ObjectKey,
    string? Bucket = null,
    TimeSpan? ExpiresIn = null);

/// <summary>Presigned URL response for browser or mobile clients.</summary>
public sealed record ObjectStoragePresignedUrl(
    string Bucket,
    string ObjectKey,
    string Url,
    string Method,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyDictionary<string, string> Headers);

/// <summary>Abstraction over an S3-compatible object store.</summary>
public interface IObjectStorageService
{
    bool IsEnabled { get; }

    Task<ObjectStorageUploadResult> UploadAsync(ObjectStorageUploadRequest request, CancellationToken cancellationToken);

    ValueTask<ObjectStoragePresignedUrl> CreatePresignedUploadUrlAsync(
        ObjectStoragePresignedUploadRequest request,
        CancellationToken cancellationToken);

    ValueTask<ObjectStoragePresignedUrl> CreatePresignedDownloadUrlAsync(
        ObjectStoragePresignedDownloadRequest request,
        CancellationToken cancellationToken);
}
