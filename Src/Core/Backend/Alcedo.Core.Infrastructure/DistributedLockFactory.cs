using Alcedo.Core.Application.Tools;
using Medallion.Threading;

namespace Alcedo.Core.Infrastructure;

internal sealed class DistributedLockFactory : IDistributedLockFactory
{
    private readonly IDistributedReaderWriterLockProvider _lockProvider;

    private const string LockKeyPrefix = "DisLock";

    public DistributedLockFactory(IDistributedReaderWriterLockProvider lockProvider)
    {
        _lockProvider = lockProvider;
    }

    public async Task<IAsyncDisposable?> TryAcquireReadLockAsync(Guid id, string idPrefix, TimeSpan timeout,
        CancellationToken ct)
    {
        var key = $"{LockKeyPrefix}:{idPrefix}:{id}";

        var @lock = _lockProvider.CreateReaderWriterLock(key);

        var handle = await @lock.TryAcquireReadLockAsync(timeout, ct);

        return handle;
    }

    public async Task<IAsyncDisposable?> TryAcquireWriteLockAsync(Guid id, string idPrefix, TimeSpan timeout,
        CancellationToken ct)
    {
        var key = $"{LockKeyPrefix}:{idPrefix}:{id}";

        var @lock = _lockProvider.CreateReaderWriterLock(key);

        var handle = await @lock.TryAcquireWriteLockAsync(timeout, ct);

        return handle;
    }
}