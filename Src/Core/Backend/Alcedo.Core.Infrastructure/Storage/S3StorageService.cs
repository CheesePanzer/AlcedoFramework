using Alcedo.Core.Domain.IStorageService;
using Amazon.S3;
using Amazon.S3.Model;

namespace Alcedo.Core.Infrastructure.Storage;

public sealed class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;

    public S3FileStorageService(IAmazonS3 s3Client)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
    }

    public async Task UploadAsync(string bucketName, string objectName, Stream content,
        string contentType = "application/octet-stream", CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectName,
            InputStream = content,
            ContentType = contentType,
        };

        await _s3Client.PutObjectAsync(request, ct);
    }

    public Task<string> GetUploadUrlAsync(string bucketName, string objectName, int expirySeconds = 600)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectName,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddSeconds(expirySeconds),
            ContentType = "application/octet-stream"
        };

        string url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    public Task<string> GetDownloadUrlAsync(string bucketName, string objectName, int expirySeconds = 3600)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectName,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddSeconds(expirySeconds)
        };

        string url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    public async Task<Stream> GetFileStreamAsync(string bucketName, string objectName, CancellationToken ct = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = objectName
        };

        var response = await _s3Client.GetObjectAsync(request, ct);
        return response.ResponseStream;
    }

    public async Task<bool> ExistsAsync(string bucketName, string objectName)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(bucketName, objectName);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task DeleteAsync(string bucketName, string objectName)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = objectName
        };

        await _s3Client.DeleteObjectAsync(request);
    }

    public async Task CopyAsync(string sourceBucket, string sourceObject, string destBucket, string destObject)
    {
        var request = new CopyObjectRequest
        {
            SourceBucket = sourceBucket,
            SourceKey = sourceObject,
            DestinationBucket = destBucket,
            DestinationKey = destObject
        };

        await _s3Client.CopyObjectAsync(request);
    }
}