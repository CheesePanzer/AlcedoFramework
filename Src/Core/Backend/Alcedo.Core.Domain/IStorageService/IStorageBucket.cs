namespace Alcedo.Core.Domain.IStorageService;

public interface IBucketSpace
{
    static abstract string BucketName { get; }
}

public sealed class GlobalPublicBucket : IBucketSpace
{
    public static string BucketName => "global-public-bucket";
}

public sealed class GlobalPrivateBucket : IBucketSpace
{
    public static string BucketName => "global-private-bucket";
}