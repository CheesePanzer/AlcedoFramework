namespace Alcedo.Core.Domain.IStorageService;

/// <summary>
/// Abstraction of storage of S3 or Azure
/// </summary>
public interface IFileStorageService
{
    Task UploadAsync(string bucketName, string objectName, Stream content,
        string contentType = "application/octet-stream", CancellationToken ct = default);

    Task<string> GetUploadUrlAsync(string bucketName, string objectName, int expirySeconds = 600);

    Task<string> GetDownloadUrlAsync(string bucketName, string objectName, int expirySeconds = 3600);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bucketName"></param>
    /// <param name="objectName"></param>
    /// <returns></returns>
    /// <remarks>using var stream = await</remarks>
    Task<Stream> GetFileStreamAsync(string bucketName, string objectName, CancellationToken ct = default);

    Task<bool> ExistsAsync(string bucketName, string objectName);

    Task DeleteAsync(string bucketName, string objectName);

    Task CopyAsync(string sourceBucket, string sourceObject, string destBucket, string destObject);
}