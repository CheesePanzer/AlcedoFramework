using Alcedo.Core.Domain.IStorageService;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Core.Infrastructure.Storage;

public static class StorageKeyPropertyBuilderExtensions
{
    private const string AnnotationName = "Alcedo:StorageKey";
    private const string OnDeleteBehavior = "Alcedo:StorageKeyOnDelete";
    private const string BucketAnnotationName = "Alcedo:StorageKeyBucket";

    public static PropertyBuilder<string?> IsNullableStorageKey(this PropertyBuilder<string?> builder, string bucket, StorageKeyOnDelete onDelete)
    {
        builder.HasAnnotation(AnnotationName, true);
        builder.HasAnnotation(BucketAnnotationName, bucket);
        builder.HasAnnotation(OnDeleteBehavior, onDelete);
        return builder;
    }
    
    public static PropertyBuilder<string> IsNonNullableStorageKey(this PropertyBuilder<string> builder, string bucket, StorageKeyOnDelete onDelete)
    {
        builder.HasAnnotation(AnnotationName, true);
        builder.HasAnnotation(BucketAnnotationName, bucket);
        builder.HasAnnotation(OnDeleteBehavior, onDelete);
        return builder;
    }

    public static bool IsStorageKey(this IProperty property)
        => property.FindAnnotation(AnnotationName) is not null;

    public static string GetStorageBucket(this IProperty property)
        => (string)property.FindAnnotation(BucketAnnotationName)!.Value!;
    
    public static StorageKeyOnDelete GetStorageKeyOnDelete(this IProperty property)
        => (StorageKeyOnDelete)property.FindAnnotation(OnDeleteBehavior)!.Value!;
}