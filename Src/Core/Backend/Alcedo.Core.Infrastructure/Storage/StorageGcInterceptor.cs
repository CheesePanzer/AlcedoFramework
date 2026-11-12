using Alcedo.Core.Domain.IStorageService;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Alcedo.Core.Infrastructure.Storage;

internal class StorageGcInterceptor : SaveChangesInterceptor
{
    private readonly IStorageKeyCache _cache;
    
    public StorageGcInterceptor(IStorageKeyCache cache)
    {
        _cache = cache;
    }
    
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CollectGcEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CollectGcEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CollectGcEntries(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var gcEntries = new List<StorageGcTask>();

        foreach (var entry in context.ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Deleted or EntityState.Modified)
                     .Where(e => _cache.HasStorageKeys(e.Entity.GetType())))
        {
            var props = _cache.GetStorageKeys(entry.Entity.GetType())!;

            foreach (var p in props.Where(prop => prop.OnDelete == StorageKeyOnDelete.Cascade).ToList())
            {
                if (entry.State == EntityState.Deleted)
                {
                    var key = (string?)p.Property.GetValue(entry.Entity);
                    if (key is not null)
                    {
                        gcEntries.Add(StorageGcTask.Create(p.Bucket, key));
                    }
                }
                else
                {
                    var originalKey = (string?)entry.Property(p.Property.Name).OriginalValue;
                    var currentKey = (string?)entry.Property(p.Property.Name).CurrentValue;
                    if (originalKey is not null && originalKey != currentKey)
                    {
                        gcEntries.Add(StorageGcTask.Create(p.Bucket, originalKey));
                    }
                }
            }
        }

        context.Set<StorageGcTask>().AddRange(gcEntries);
    }
}