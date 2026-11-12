using System.Diagnostics;
using Alcedo.Core.Domain;
using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.Entity;
using Microsoft.Extensions.Logging;

namespace Alcedo.Core.Application.Tools;

internal struct LockTimeoutTracker
{
    private readonly long _startTimestamp;
    private readonly double _totalLimitSecs;
    private readonly double _limitPerLockSecs;
    private readonly bool _isNoWait;

    public LockTimeoutTracker(int timeoutSecsPerLock, int totalTimeoutSecs, bool isNoWait)
    {
        _isNoWait = isNoWait;

        if (_isNoWait)
        {
            _startTimestamp = 0;
            _totalLimitSecs = 0;
            _limitPerLockSecs = 0;
        }
        else
        {
            _startTimestamp = Stopwatch.GetTimestamp();
            _limitPerLockSecs = timeoutSecsPerLock <= 0 ? 1 : timeoutSecsPerLock;
            _totalLimitSecs = totalTimeoutSecs <= 0 ? 1 : totalTimeoutSecs;
        }
    }

    public TimeSpan GetNextLockTimeLimit()
    {
        if (_isNoWait)
        {
            return TimeSpan.Zero;
        }

        var elapsedSecs = Stopwatch.GetElapsedTime(_startTimestamp).TotalSeconds;
        var timeLeft = _totalLimitSecs - elapsedSecs;

        if (timeLeft <= 0)
        {
            return TimeSpan.FromSeconds(timeLeft);
        }

        var limit = Math.Min(_limitPerLockSecs, timeLeft);
        return TimeSpan.FromSeconds(limit);
    }
}

public class ApplicationLocks : IAsyncDisposable
{
    private readonly List<IAsyncDisposable> _handles;

    public ApplicationLocks(List<IAsyncDisposable> handles)
    {
        _handles = handles;
    }

    public async ValueTask DisposeAsync()
    {
        for (int i = _handles.Count - 1; i >= 0; i--)
        {
            await _handles[i].DisposeAsync();
        }
    }
}

internal enum LockType
{
    ReadLock = 0,
    WriteLock = 1,
}

internal record LockRequest(Guid Id, string Prefix, LockType LockType);

public interface IDistributedLockFactory
{
    Task<IAsyncDisposable?> TryAcquireReadLockAsync(Guid id, string idPrefix, TimeSpan timeout, CancellationToken ct);

    Task<IAsyncDisposable?> TryAcquireWriteLockAsync(Guid id, string idPrefix, TimeSpan timeout, CancellationToken ct);
}

internal sealed class ApplicationLockFactory
{
    private readonly IDistributedLockFactory _lockFactory;
    private readonly ILogger<ApplicationLockFactory> _logger;
    private const int DefaultTimeoutSecondsPerLock = 2;
    private const int DefaultTotalTimeoutSeconds = 5;

    internal ApplicationLockFactory(IDistributedLockFactory lockFactory, ILogger<ApplicationLockFactory> logger)
    {
        _lockFactory = lockFactory;
        _logger = logger;
    }

    public async Task<ApplicationLocks> BatchLockAsync(
        IReadOnlyCollection<LockRequest> lockRequests,
        int? timeoutSecsPerLock = null,
        int? totalTimeoutSecs = null,
        bool isNoWait = false,
        CancellationToken ct = default)
    {
        if (lockRequests is null || !lockRequests.Any())
        {
            throw new ArgumentException("Need At Least One Id");
        }

        var sortedRqs = lockRequests
            .GroupBy(x => (x.Id, x.Prefix))
            .Select(g => new LockRequest(
                g.Key.Id,
                g.Key.Prefix,
                g.Any(r => r.LockType == LockType.WriteLock) ? LockType.WriteLock : LockType.ReadLock))
            .OrderBy(x => x.Prefix)
            .ThenBy(x => x.Id)
            .ToList();

        var handles = new List<IAsyncDisposable>();

        try
        {
            var tracker = new LockTimeoutTracker(timeoutSecsPerLock ?? DefaultTimeoutSecondsPerLock,
                totalTimeoutSecs ?? DefaultTotalTimeoutSeconds, isNoWait);

            foreach (var req in sortedRqs)
            {
                ct.ThrowIfCancellationRequested();

                var lockLimit = tracker.GetNextLockTimeLimit();

                if (lockLimit < TimeSpan.Zero)
                {
                    throw new DomErrLockConflict($"{req.Prefix} with Id: {req.Id}", req.LockType.ToString(), lockLimit);
                }

                var handle = req.LockType == LockType.WriteLock
                    ? await _lockFactory.TryAcquireWriteLockAsync(req.Id, req.Prefix, lockLimit, ct)
                    : await _lockFactory.TryAcquireReadLockAsync(req.Id, req.Prefix, lockLimit, ct);

                if (handle is null)
                {
                    throw new DomErrLockConflict($"{req.Prefix} with Id: {req.Id}", req.LockType.ToString(), lockLimit);
                }

                handles.Add(handle);
            }

            return new ApplicationLocks(handles);
        }
        catch
        {
            for (var i = handles.Count - 1; i >= 0; i--)
            {
                try
                {
                    await handles[i].DisposeAsync();
                }
                catch (Exception disposeEx)
                {
                    _logger.LogError(disposeEx, "Failed to release lock during cleanup");
                }
            }

            throw;
        }
    }
}

public sealed class ApplicationLockBuilder
{
    private readonly ApplicationLockFactory _factory;
    private readonly List<LockRequest> _requests = new();

    internal ApplicationLockBuilder(ApplicationLockFactory factory) => _factory = factory;

    private string ToRedisKey(string prefix, string? additionalPrefix)
    {
        if (additionalPrefix is not null)
        {
            return prefix.Trim(':') + ":" + additionalPrefix.Trim(':');
        }

        return prefix;
    }

    public ApplicationLockBuilder AddX(string identity, IEnumerable<Guid> ids, string? scope = null)
    {
        var prefix = ToRedisKey(identity, scope);
        _requests.AddRange(ids.Select(id => new LockRequest(id, prefix, LockType.WriteLock)));
        return this;
    }

    public ApplicationLockBuilder AddS(string identity, IEnumerable<Guid> ids, string? scope = null)
    {
        var prefix = ToRedisKey(identity, scope);
        _requests.AddRange(ids.Select(id => new LockRequest(id, prefix, LockType.ReadLock)));
        return this;
    }

    public ApplicationLockBuilder AddX(string identity, Guid id, string? scope = null) => AddX(identity, [id], scope);
    public ApplicationLockBuilder AddS(string identity, Guid id, string? scope = null) => AddS(identity, [id], scope);

    public ApplicationLockBuilder AddX<TAgg>(IEnumerable<Guid> ids, string? scope = null) where TAgg : AggregateRoot
        => AddX(AggregateRootMetadataCenter.GetMetaData<TAgg>().Identity, ids, scope);

    public ApplicationLockBuilder AddS<TAgg>(IEnumerable<Guid> ids, string? scope = null) where TAgg : AggregateRoot
        => AddS(AggregateRootMetadataCenter.GetMetaData<TAgg>().Identity, ids, scope);

    public ApplicationLockBuilder AddX<TAgg>(Guid id, string? scope = null) where TAgg : AggregateRoot =>
        AddX<TAgg>([id], scope);

    public ApplicationLockBuilder AddS<TAgg>(Guid id, string? scope = null) where TAgg : AggregateRoot =>
        AddS<TAgg>([id], scope);

    public Task<ApplicationLocks> LockNoWaitAsync(CancellationToken ct = default)
        => _factory.BatchLockAsync(_requests, isNoWait: true, ct: ct);

    public Task<ApplicationLocks> LockAsync(int? perLockSecs = null, int? totalSecs = null,
        CancellationToken ct = default)
        => _factory.BatchLockAsync(_requests, perLockSecs, totalSecs, ct: ct);
}

public sealed class ApplicationLockProvider
{
    private readonly ApplicationLockFactory _factory;

    public ApplicationLockProvider(IDistributedLockFactory factory, ILoggerFactory loggerFactory)
    {
        _factory = new ApplicationLockFactory(
            factory,
            loggerFactory.CreateLogger<ApplicationLockFactory>());
    }

    public ApplicationLockBuilder CreateLock() => new(_factory);
}