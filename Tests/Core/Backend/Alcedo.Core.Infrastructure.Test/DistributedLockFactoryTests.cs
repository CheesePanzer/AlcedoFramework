using Medallion.Threading;

namespace Alcedo.Core.Infrastructure.Test;

public class FakeSyncHandle : IAsyncDisposable, IDisposable, IDistributedSynchronizationHandle
{
    public bool Disposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return default;
    }

    public void Dispose() => Disposed = true;
    public System.Threading.CancellationToken HandleLostToken => default;
}

public class FakeRwLock : IDistributedReaderWriterLock
{
    private readonly bool _succeedRead;
    private readonly bool _succeedWrite;

    public string Name { get; }

    public FakeRwLock(string name = "fake", bool succeedRead = true, bool succeedWrite = true)
    {
        Name = name;
        _succeedRead = succeedRead;
        _succeedWrite = succeedWrite;
    }

    public IDistributedSynchronizationHandle? TryAcquireReadLock(System.TimeSpan timeout,
        System.Threading.CancellationToken cancellationToken = default)
        => _succeedRead ? new FakeSyncHandle() : null;

    public IDistributedSynchronizationHandle AcquireReadLock(System.TimeSpan? timeout = null,
        System.Threading.CancellationToken cancellationToken = default)
        => _succeedRead ? new FakeSyncHandle() : throw new TimeoutException();

    public System.Threading.Tasks.ValueTask<IDistributedSynchronizationHandle> AcquireReadLockAsync(
        System.TimeSpan? timeout = null, System.Threading.CancellationToken cancellationToken = default)
        => new(_succeedRead ? new FakeSyncHandle() : throw new TimeoutException());

    public System.Threading.Tasks.ValueTask<IDistributedSynchronizationHandle?> TryAcquireReadLockAsync(
        System.TimeSpan? timeout = null, System.Threading.CancellationToken cancellationToken = default)
        => new(_succeedRead ? new FakeSyncHandle() : null);

    public System.Threading.Tasks.ValueTask<IDistributedSynchronizationHandle?> TryAcquireReadLockAsync(
        System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken)
        => new(_succeedRead ? new FakeSyncHandle() : null);

    public IDistributedSynchronizationHandle? TryAcquireWriteLock(System.TimeSpan timeout,
        System.Threading.CancellationToken cancellationToken = default)
        => _succeedWrite ? new FakeSyncHandle() : null;

    public IDistributedSynchronizationHandle AcquireWriteLock(System.TimeSpan? timeout = null,
        System.Threading.CancellationToken cancellationToken = default)
        => _succeedWrite ? new FakeSyncHandle() : throw new TimeoutException();

    public System.Threading.Tasks.ValueTask<IDistributedSynchronizationHandle> AcquireWriteLockAsync(
        System.TimeSpan? timeout = null, System.Threading.CancellationToken cancellationToken = default)
        => new(_succeedWrite ? new FakeSyncHandle() : throw new TimeoutException());

    public System.Threading.Tasks.ValueTask<IDistributedSynchronizationHandle?> TryAcquireWriteLockAsync(
        System.TimeSpan? timeout = null, System.Threading.CancellationToken cancellationToken = default)
        => new(_succeedWrite ? new FakeSyncHandle() : null);

    public System.Threading.Tasks.ValueTask<IDistributedSynchronizationHandle?> TryAcquireWriteLockAsync(
        System.TimeSpan timeout, System.Threading.CancellationToken cancellationToken)
        => new(_succeedWrite ? new FakeSyncHandle() : null);
}

public class FakeLockProvider : IDistributedReaderWriterLockProvider
{
    public string? LastKey { get; private set; }

    public IDistributedReaderWriterLock CreateReaderWriterLock(string name)
    {
        LastKey = name;
        return new FakeRwLock(name);
    }
}

public class DistributedLockFactoryTests
{
    [Fact]
    public async Task TryAcquireReadLockAsync_KeyFormat_Correct()
    {
        var provider = new FakeLockProvider();
        var factory = new DistributedLockFactory(provider);
        var id = Guid.NewGuid();

        await factory.TryAcquireReadLockAsync(id, "order", TimeSpan.FromSeconds(1), default);

        Assert.Equal($"DisLock:order:{id}", provider.LastKey);
    }

    [Fact]
    public async Task TryAcquireWriteLockAsync_KeyFormat_Correct()
    {
        var provider = new FakeLockProvider();
        var factory = new DistributedLockFactory(provider);
        var id = Guid.NewGuid();

        await factory.TryAcquireWriteLockAsync(id, "product", TimeSpan.FromSeconds(1), default);

        Assert.Equal($"DisLock:product:{id}", provider.LastKey);
    }

    [Fact]
    public async Task TryAcquireReadLockAsync_ReturnsHandle_WhenSucceeds()
    {
        var provider = new FakeLockProvider();
        var factory = new DistributedLockFactory(provider);

        var handle = await factory.TryAcquireReadLockAsync(Guid.NewGuid(), "x", TimeSpan.FromSeconds(1), default);

        Assert.NotNull(handle);
    }

    [Fact]
    public async Task TryAcquireWriteLockAsync_ReturnsHandle_WhenSucceeds()
    {
        var provider = new FakeLockProvider();
        var factory = new DistributedLockFactory(provider);

        var handle = await factory.TryAcquireWriteLockAsync(Guid.NewGuid(), "x", TimeSpan.FromSeconds(1), default);

        Assert.NotNull(handle);
    }
}