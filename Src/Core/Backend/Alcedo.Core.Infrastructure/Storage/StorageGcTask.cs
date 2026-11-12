using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Core.Infrastructure.Storage;

public class StorageGcTask
{
    public long Id { get; private set; }

    public string Bucket { get; private set; }

    public string ObjectKey { get; private set; }

    public DateTimeOffset ScheduledAt { get; private set; }

    public int RetryCount { get; private set; }

    public string? LastError { get; private set; }

    private StorageGcTask()
    {
    }

    public static StorageGcTask Create(string bucket, string objectKey)
        => new() { Bucket = bucket, ObjectKey = objectKey, ScheduledAt = DateTime.UtcNow };
}

public class StorageGcEntryConfiguration : IEntityTypeConfiguration<StorageGcTask>
{
    public void Configure(EntityTypeBuilder<StorageGcTask> builder)
    {
        builder.ToTable("storage_gc");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LastError).HasColumnType("TEXT");
    }
}