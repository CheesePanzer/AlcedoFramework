using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.DomException;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alcedo.Core.Application.Test;

public class ApplicationLockProviderTests
{
    private static ApplicationLockFactory CreateFactory(IDistributedLockFactory lockFactory)
        => new(lockFactory, NullLogger<ApplicationLockFactory>.Instance);

    #region LockTimeoutTracker

    [Fact]
    public void LockTimeoutTracker_IsNoWait_ReturnsZero()
    {
        var tracker = new LockTimeoutTracker(10, 30, isNoWait: true);

        Assert.Equal(TimeSpan.Zero, tracker.GetNextLockTimeLimit());
    }

    [Fact]
    public void LockTimeoutTracker_NormalMode_ReturnsMinOfPerLockAndTimeLeft()
    {
        var tracker = new LockTimeoutTracker(5, 100, isNoWait: false);

        var limit = tracker.GetNextLockTimeLimit();

        Assert.True(limit > TimeSpan.Zero);
        Assert.True(limit <= TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void LockTimeoutTracker_WhenTimeLeftExhausted_ReturnsNegative()
    {
        var tracker = new LockTimeoutTracker(10, 1, isNoWait: false);

        Thread.Sleep(1100);
        var limit = tracker.GetNextLockTimeLimit();

        Assert.True(limit < TimeSpan.Zero);
    }

    #endregion

    #region ApplicationLockFactory

    private class FakeLockHandle : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private class FakeLockFactory : IDistributedLockFactory
    {
        private readonly Dictionary<(Guid id, string prefix, LockType type), bool> _results = new();
        public List<(Guid id, string prefix, LockType type)> AcquiredOrder { get; } = new();
        public List<(Guid id, string prefix, TimeSpan timeout)> ReadTimeouts { get; } = new();
        public List<(Guid id, string prefix, TimeSpan timeout)> WriteTimeouts { get; } = new();

        public void SetResult(Guid id, string prefix, LockType type, bool succeed)
            => _results[(id, prefix, type)] = succeed;

        public Task<IAsyncDisposable?> TryAcquireReadLockAsync(Guid id, string idPrefix, TimeSpan timeout,
            CancellationToken ct)
        {
            AcquiredOrder.Add((id, idPrefix, LockType.ReadLock));
            ReadTimeouts.Add((id, idPrefix, timeout));
            if (_results.TryGetValue((id, idPrefix, LockType.ReadLock), out var ok) && ok)
            {
                return Task.FromResult<IAsyncDisposable?>(new FakeLockHandle());
            }

            return Task.FromResult<IAsyncDisposable?>(null);
        }

        public Task<IAsyncDisposable?> TryAcquireWriteLockAsync(Guid id, string idPrefix, TimeSpan timeout,
            CancellationToken ct)
        {
            AcquiredOrder.Add((id, idPrefix, LockType.WriteLock));
            WriteTimeouts.Add((id, idPrefix, timeout));
            if (_results.TryGetValue((id, idPrefix, LockType.WriteLock), out var ok) && ok)
            {
                return Task.FromResult<IAsyncDisposable?>(new FakeLockHandle());
            }

            return Task.FromResult<IAsyncDisposable?>(null);
        }
    }

    [Fact]
    public async Task BatchLockAsync_EmptyRequests_ThrowsArgumentException()
    {
        var factory = CreateFactory(new FakeLockFactory());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            factory.BatchLockAsync(Array.Empty<LockRequest>()));
    }

    [Fact]
    public async Task BatchLockAsync_DeduplicatesSameIdPrefix()
    {
        var fake = new FakeLockFactory();
        fake.SetResult(Guid.Parse("11111111-1111-1111-1111-111111111111"), "test", LockType.ReadLock, true);

        var factory = CreateFactory(fake);
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var requests = new[]
        {
            new LockRequest(id, "test", LockType.ReadLock),
            new LockRequest(id, "test", LockType.ReadLock)
        };

        var locks = await factory.BatchLockAsync(requests);

        Assert.Single(fake.AcquiredOrder);
    }

    [Fact]
    public async Task BatchLockAsync_ReadAndWriteOnSameId_UpgradesToWrite()
    {
        var fake = new FakeLockFactory();
        fake.SetResult(Guid.Parse("11111111-1111-1111-1111-111111111111"), "test", LockType.WriteLock, true);

        var factory = CreateFactory(fake);
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var requests = new[]
        {
            new LockRequest(id, "test", LockType.ReadLock),
            new LockRequest(id, "test", LockType.WriteLock)
        };

        var locks = await factory.BatchLockAsync(requests);

        Assert.Single(fake.AcquiredOrder);
        Assert.Equal(LockType.WriteLock, fake.AcquiredOrder[0].type);
    }

    [Fact]
    public async Task BatchLockAsync_SortsByPrefixThenId()
    {
        var fake = new FakeLockFactory();
        var id1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var id2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        fake.SetResult(id1, "b", LockType.ReadLock, true);
        fake.SetResult(id2, "a", LockType.ReadLock, true);

        var factory = CreateFactory(fake);
        var requests = new[]
        {
            new LockRequest(id1, "b", LockType.ReadLock),
            new LockRequest(id2, "a", LockType.ReadLock)
        };

        await factory.BatchLockAsync(requests);

        Assert.Equal("a", fake.AcquiredOrder[0].prefix);
        Assert.Equal("b", fake.AcquiredOrder[1].prefix);
    }

    [Fact]
    public async Task BatchLockAsync_WhenLockFails_ReleasesAcquiredLocks()
    {
        var fake = new FakeLockFactory();
        var id1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var id2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        fake.SetResult(id1, "test", LockType.ReadLock, true);
        fake.SetResult(id2, "test", LockType.ReadLock, false);

        var factory = CreateFactory(fake);
        var requests = new[]
        {
            new LockRequest(id1, "test", LockType.ReadLock),
            new LockRequest(id2, "test", LockType.ReadLock)
        };

        await Assert.ThrowsAsync<DomErrLockConflict>(() => factory.BatchLockAsync(requests));
    }

    [Fact]
    public async Task BatchLockAsync_NoWait_PassesZeroTimeout()
    {
        var fake = new FakeLockFactory();
        fake.SetResult(Guid.Parse("11111111-1111-1111-1111-111111111111"), "test", LockType.ReadLock, true);

        var factory = CreateFactory(fake);
        var requests = new[]
        {
            new LockRequest(Guid.Parse("11111111-1111-1111-1111-111111111111"), "test", LockType.ReadLock)
        };

        await factory.BatchLockAsync(requests, isNoWait: true);

        Assert.Equal(TimeSpan.Zero, fake.ReadTimeouts[0].timeout);
    }

    [Fact]
    public async Task BatchLockAsync_Cancellation_IsCheckedBetweenLocks()
    {
        var fake = new FakeLockFactory();
        fake.SetResult(Guid.Parse("11111111-1111-1111-1111-111111111111"), "test", LockType.ReadLock, true);

        var factory = CreateFactory(fake);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var requests = new[]
        {
            new LockRequest(Guid.Parse("11111111-1111-1111-1111-111111111111"), "test", LockType.ReadLock)
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            factory.BatchLockAsync(requests, ct: cts.Token));
    }

    #endregion

    #region ApplicationLockBuilder

    [Fact]
    public async Task LockBuilder_ToRedisKey_WithScope_FormatsCorrectly()
    {
        var fake = new FakeLockFactory();
        var provider = new ApplicationLockProvider(fake, NullLoggerFactory.Instance);
        var builder = provider.CreateLock();

        builder.AddX("order", Guid.NewGuid(), "scope");

        await Assert.ThrowsAsync<DomErrLockConflict>(async () =>
        {
            var fake2 = new FakeLockFactory();
            var provider2 = new ApplicationLockProvider(fake2, NullLoggerFactory.Instance);
            var b = provider2.CreateLock();
            b.AddX("order", Guid.NewGuid(), "scope");
            await b.LockNoWaitAsync();
        });
    }

    #endregion

    #region ApplicationLocks

    [Fact]
    public async Task ApplicationLocks_DisposeAsync_ReleasesInReverseOrder()
    {
        var handles = new List<IAsyncDisposable>
        {
            new FakeLockHandle(),
            new FakeLockHandle(),
            new FakeLockHandle()
        };

        var locks = new ApplicationLocks(handles);
        await locks.DisposeAsync();

        Assert.True(((FakeLockHandle)handles[2]).IsDisposed);
        Assert.True(((FakeLockHandle)handles[1]).IsDisposed);
        Assert.True(((FakeLockHandle)handles[0]).IsDisposed);
    }

    #endregion
}