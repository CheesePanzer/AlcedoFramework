using Alcedo.Core.Domain.IStorageService;
using Alcedo.Core.Infrastructure.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alcedo.Core.Infrastructure.Storage;

public interface IStorageGcExecutor
{
    Task ExecuteAsync(int batchSize = 100, CancellationToken cancellationToken = default);
}

internal class StorageGcExecutor : IStorageGcExecutor
{
    private readonly AlcedoDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<StorageGcExecutor> _logger;

    public StorageGcExecutor(AlcedoDbContext context, IFileStorageService fileStorage,
        ILogger<StorageGcExecutor> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task ExecuteAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var tasks = await _context.Set<StorageGcTask>()
            .FromSqlRaw("""
                        SELECT * FROM storage_gc
                        ORDER BY id
                        LIMIT {0}
                        FOR UPDATE SKIP LOCKED
                        """, batchSize)
            .ToListAsync(cancellationToken);

        if (tasks.Count == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        foreach (var task in tasks)
        {
            try
            {
                await _fileStorage.DeleteAsync(task.Bucket, task.ObjectKey);
                _context.Set<StorageGcTask>().Remove(task);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete storage object {Bucket}/{ObjectKey}", task.Bucket, task.ObjectKey);
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE storage_gc SET retry_count = retry_count + 1, last_error = {0} WHERE id = {1}",
                    ex.Message, task.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}