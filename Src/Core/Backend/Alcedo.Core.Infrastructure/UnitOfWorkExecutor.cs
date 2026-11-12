using Alcedo.Core.Application;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.Event;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alcedo.Core.Infrastructure;

internal sealed class UnitOfWorkExecutor : IUnitOfWorkExecutor
{
    private readonly AggregateEventsCollector _evtCollector;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly TransactionManager _transactionManager;
    private readonly List<ApplicationEventBase> _contextAppEvents = new();
    private readonly ILogger<UnitOfWorkExecutor> _logger;

    public UnitOfWorkExecutor(AggregateEventsCollector evtCollector, TransactionManager transactionManager,
        IPublishEndpoint publishEndpoint, ILogger<UnitOfWorkExecutor> logger)
    {
        _evtCollector = evtCollector;
        _transactionManager = transactionManager;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    #region No Return Value

    public Task ExecuteAsync(Action action, CancellationToken ct = default)
        => ExecuteAsync(() =>
        {
            action();
            return Task.CompletedTask;
        }, ct);

    public Task ExecuteAsync(Func<Task> action, CancellationToken ct = default)
        => ExecuteAsync(_ => action(), ct);

    public Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
        => ExecuteAsync<object>(async token =>
        {
            await action(token);
            return null!;
        }, ct);

    #endregion

    #region Has Return Value

    public Task<T> ExecuteAsync<T>(Func<T> action, CancellationToken ct = default)
        => ExecuteAsync(_ => Task.FromResult(action()), ct);

    public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
        => ExecuteAsync(_ => action(), ct);

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
    {
        string methodName = action.Method.Name;
        await UowBegin(methodName, ct);
        try
        {
            var result = await action(ct);
            await UowComplete(methodName, ct);
            return result;
        }
        catch (Exception ex)
        {
            await UowHandleError(ex, methodName);
            throw;
        }
        finally
        {
            UowClear();
        }
    }

    #endregion

    #region Retry No Return Value

    public Task ExecuteWithRetryAsync(Action action, int maxRetries = 3, CancellationToken ct = default)
        => ExecuteWithRetryAsync(() =>
        {
            action();
            return Task.CompletedTask;
        }, maxRetries, ct);

    public Task ExecuteWithRetryAsync(Func<Task> action, int maxRetries = 3, CancellationToken ct = default)
        => ExecuteWithRetryAsync(_ => action(), maxRetries, ct);

    public Task ExecuteWithRetryAsync(Func<CancellationToken, Task> action, int maxRetries = 3,
        CancellationToken ct = default)
        => ExecuteWithRetryAsync<object>(async token =>
        {
            await action(token);
            return null!;
        }, maxRetries, ct);

    #endregion

    #region Retry Has Return Value

    public Task<T> ExecuteWithRetryAsync<T>(Func<T> action, int maxRetries = 3, CancellationToken ct = default)
        => ExecuteWithRetryAsync(_ => Task.FromResult(action()), maxRetries, ct);

    public Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 3, CancellationToken ct = default)
        => ExecuteWithRetryAsync(_ => action(), maxRetries, ct);

    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> action,
        int maxRetries = 3,
        CancellationToken ct = default)
    {
        for (var i = 0; i <= maxRetries; i++)
        {
            try
            {
                return await ExecuteAsync(action, ct);
            }
            catch (DbUpdateConcurrencyException) when (i < maxRetries)
            {
                var delayMs = 100 * (1 << i) + Random.Shared.Next(50);
                _logger.LogWarning(
                    "UowError:Optimistic concurrency conflict in {Method}, retry {Attempt}/{Max} after {Delay}ms",
                    action.Method.Name, i + 1, maxRetries, delayMs);
                await Task.Delay(delayMs, ct);
            }
        }

        throw new InvalidOperationException("UowRetryError:Should not reach here");
    }

    #endregion

    private async Task UowBegin(string methodName, CancellationToken ct = default)
    {
        _logger.LogInformation("UowBegin:{MethodName}", methodName);
        await _transactionManager.BeginTransactionAsync(ct);
    }

    private async Task UowComplete(string methodName, CancellationToken ct = default)
    {
        var aggEvts = _evtCollector.CollectAggregateEvents();
        var appEvts = CollectAppEvents();
        await _publishCollectedEvents(aggEvts, appEvts, ct);
        _logger.LogInformation("UowEventsPublished:{MethodName}---DomainEvents:{AggCount};AppEvents:{AppCount}",
            methodName, aggEvts.Count, appEvts.Count);
        await _transactionManager.CommitAsync();
        _logger.LogInformation("UowCommitted:{MethodName}", methodName);
    }

    private async Task UowHandleError(Exception ex, string methodName)
    {
        try
        {
            await _transactionManager.RollbackAsync();
            _logger.LogInformation("UowRollback:{MethodName}", methodName);
        }
        catch (Exception rollbackEx)
        {
            _logger.LogCritical(rollbackEx, "UowRollbackFailed:{MethodName}. OriginalError:{Error}",
                methodName, ex.Message);

            if (!ex.Data.IsReadOnly)
            {
                ex.Data["RollbackException"] = rollbackEx.Message;
            }
        }
    }

    private void UowClear()
    {
        _evtCollector.ClearAllEvents();
        _contextAppEvents.Clear();
    }


    public Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
        => _transactionManager.CreateSavepointAsync(name, cancellationToken);

    public Task RollbackToSavepointAsync(string name)
        => _transactionManager.RollbackToSavepointAsync(name);

    public Task ReleaseSavepointAsync(string name)
        => _transactionManager.ReleaseSavepointAsync(name);

    public void AddApplicationEvent(ApplicationEventBase appEvent)
    {
        _contextAppEvents.Add(appEvent);
    }

    public void AddApplicationEvents(IEnumerable<ApplicationEventBase> appEvent)
    {
        _contextAppEvents.AddRange(appEvent);
    }

    private IReadOnlyList<ApplicationEventBase> CollectAppEvents()
    {
        if (_contextAppEvents.Count == 0)
        {
            return [];
        }

        var snapshot = _contextAppEvents.ToList();
        _contextAppEvents.Clear();

        return snapshot;
    }

    private async Task _publishCollectedEvents(IReadOnlyList<DomainEventBase> aggregateEvents,
        IReadOnlyList<ApplicationEventBase> applicationEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var @event in aggregateEvents)
        {
            await _publishEndpoint.Publish(@event, @event.GetType(), cancellationToken);
        }

        foreach (var appEvent in applicationEvents)
        {
            await _publishEndpoint.Publish(appEvent, appEvent.GetType(), cancellationToken);
        }
    }
}