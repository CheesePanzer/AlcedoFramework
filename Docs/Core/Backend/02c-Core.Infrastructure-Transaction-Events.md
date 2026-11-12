# Alcedo.Core.Infrastructure — Unit Of Work

This document covers the transaction boundary (`UnitOfWorkExecutor`), the internal `TransactionManager`, and the event collection and publishing pipeline.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [IUnitOfWorkExecutor](#iunitofworkexecutor)
  - [Execute Overloads](#execute-overloads)
  - [Savepoints](#savepoints)
  - [Application Events](#application-events)
- [TransactionManager](#transactionmanager)
- [AggregateEventsCollector](#aggregateeventscollector)
- [DomainEventPublisher](#domaineventpublisher)
- [Event Flow](#event-flow)
- [Design Principles](#design-principles)

---

## Architecture Overview

Alcedo wraps every business operation in a **Unit of Work** that combines three responsibilities into a single atomic boundary:

1. **Transaction** — database transaction via EF Core
2. **Change Persistence** — `SaveChanges` called at commit time
3. **Domain Event Publishing** — events collected from aggregates and published via MassTransit

| Component | Responsibility |
|-----------|---------------|
| `IUnitOfWorkExecutor` | Public API for wrapping operations |
| `TransactionManager` | Low-level transaction lifecycle (begin / commit / rollback / savepoints) |
| `AggregateEventsCollector` | Gathers `DomainEventBase` instances from tracked `AggregateRoot`s |
| `DomainEventPublisher` | Publishes a single domain event via MassTransit |

---

## IUnitOfWorkExecutor

The primary interface application code uses to demarcate transaction boundaries.

```csharp
public interface IUnitOfWorkExecutor
{
    Task ExecuteAsync(Action action, CancellationToken ct = default);
    Task ExecuteAsync(Func<Task> action, CancellationToken ct = default);
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);

    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);

    Task<TResult> ExecuteAsync<T, TResult>(Func<Task<T>> action, Func<T, TResult> selector,
        CancellationToken ct = default);
    Task<TResult> ExecuteAsync<T, TResult>(Func<CancellationToken, Task<T>> action, Func<T, TResult> selector,
        CancellationToken ct = default);

    Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default);
    Task RollbackToSavepointAsync(string name);
    Task ReleaseSavepointAsync(string name);

    Task PublishApplicationEvent(ApplicationEventBase domainEvent, CancellationToken cancellationToken = default);
}
```

### Execute Overloads

All `ExecuteAsync` overloads follow the same pattern:

```csharp
await _unitOfWork.ExecuteAsync(async ct =>
{
    var user = await _userStore.GetByIdForUpdateAsync(userId);
    user.Update("New Name", operId, operSnap);
    // No SaveChanges here — happens at commit
}, cancellationToken);
```

**What happens inside the boundary:**

1. `BeginTransactionAsync` — starts a new EF Core transaction (no-op if already started)
2. `action` — your business logic runs; entities are added/modified/removed via stores
3. `CollectAggregateEvents` — scans the `ChangeTracker` for all `AggregateRoot` instances and drains their event queues
4. `Publish` — all collected domain events are published via MassTransit
5. `CommitAsync` — calls `SaveChangesAsync`, then commits the transaction

If any step throws, the transaction is rolled back and the `ChangeTracker` is cleared.

**Overload guide:**

| Overload | Use When |
|----------|----------|
| `Action` | Synchronous logic inside async boundary |
| `Func<Task>` | Simple async logic, no return value |
| `Func<CancellationToken, Task>` | Async logic that needs cancellation token |
| `Func<Task<T>>` | Async logic that returns a value |
| `Func<CancellationToken, Task<T>>` | Async logic with token and return value |
| `Func<Task<T>>` + `selector` | Return a transformed result (e.g. project entity to DTO before events are cleared) |

The `T, TResult` overload is useful when you need the result **before** the transaction commits but **after** the entity work is done — for example, mapping an entity to a response DTO while navigation properties are still loaded.

### Savepoints

PostgreSQL savepoints allow partial rollback within a transaction:

```csharp
await _unitOfWork.ExecuteAsync(async ct =>
{
    await _unitOfWork.CreateSavepointAsync("before_risky_op", ct);
    try
    {
        await DoRiskyOperationAsync(ct);
    }
    catch
    {
        await _unitOfWork.RollbackToSavepointAsync("before_risky_op");
        // Continue with fallback logic
    }
    finally
    {
        await _unitOfWork.ReleaseSavepointAsync("before_risky_op");
    }
}, ct);
```

Savepoints delegate directly to `TransactionManager`, which delegates to the underlying `IDbContextTransaction`.

### Application Events

`PublishApplicationEvent` sends an `ApplicationEventBase` immediately via MassTransit. It does **not** participate in the Unit of Work boundary and does not wait for commit.

```csharp
await _unitOfWork.PublishApplicationEvent(new UserActivityEvent { UserId = userId }, ct);
```

Use this for fire-and-forget notifications that should not block the transaction or be tied to its success.

---

## TransactionManager

Internal class that wraps EF Core's `IDbContextTransaction`. It is scoped per request and tracks a single active transaction.

```csharp
internal sealed class TransactionManager
{
    public async Task BeginTransactionAsync(CancellationToken ct = default);
    public async Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken ct = default);
    public async Task<int> CommitAsync();
    public async Task RollbackAsync();
    public void ClearTracker();
    // ... savepoint methods
}
```

**Key behaviors:**

- **Idempotent begin:** Calling `BeginTransactionAsync` when a transaction is already active is a no-op.
- **Commit does SaveChanges:** `CommitAsync` first calls `DbContext.SaveChangesAsync()`, then commits the database transaction. The returned `int` is the number of rows affected.
- **Rollback clears tracker:** On rollback, `ChangeTracker.Clear()` is called to detach all entities. This prevents stale tracked instances from leaking into subsequent operations within the same scope.
- **Disposal:** Implements both `IDisposable` and `IAsyncDisposable` to ensure the underlying transaction is released.

---

## AggregateEventsCollector

Collects domain events from all tracked `AggregateRoot` instances in the `ChangeTracker`.

```csharp
internal sealed class AggregateEventsCollector
{
    public IReadOnlyList<DomainEventBase> CollectAggregateEvents()
}
```

**How it works:**

1. Queries `ChangeTracker.Entries<AggregateRoot>()` to find every tracked aggregate
2. Calls `CollectEventsAndClear()` on each aggregate
3. Returns the flattened list of all events

`CollectEventsAndClear()` is defined on `AggregateRoot`:

```csharp
public virtual ICollection<DomainEventBase> CollectEventsAndClear()
{
    var events = _events.ToList();
    _events.Clear();
    return events;
}
```

Events are copied to a new list and the internal queue is cleared. This means:
- Events are collected **once** per Unit of Work
- If the same aggregate is modified again in a second Unit of Work within the same scope, it can raise new events

---

## DomainEventPublisher

A thin wrapper over MassTransit's `IPublishEndpoint` for publishing single domain events outside of the Unit of Work boundary.

```csharp
public interface IDomainEventPublisher
{
    Task Publish(DomainEventBase domainEvent, CancellationToken ct = default);
}
```

```csharp
internal sealed class DomainEventPublisher : IDomainEventPublisher
{
    public Task Publish(DomainEventBase domainEvent, CancellationToken ct = default)
        => _publishEndpoint.Publish(domainEvent, domainEvent.GetType(), ct);
}
```

This is used when you need to publish a domain event **outside** of `UnitOfWorkExecutor.ExecuteAsync` — for example, in a background job or read-side path where no aggregate modification occurs.

Inside the Unit of Work boundary, publishing is handled directly by `UnitOfWorkExecutor` and does not require `IDomainEventPublisher`.

---

## Event Flow

### Domain Events vs Application Events

| | Domain Event | Application Event |
|--|--------------|-------------------|
| Base type | `DomainEventBase` | `ApplicationEventBase` |
| Origin | AggregateRoot business logic | Application / service layer |
| Lifecycle | Collected at UoW commit | Published immediately |
| Coupling to transaction | Yes — only published if commit succeeds | No — fire-and-forget |
| Example | `UserCreatedEvent` | `UserLoggedInNotification` |

### Full Flow Example

```csharp
// 1. Domain method raises an event
public void Activate()
{
    Status = MasterDataStates.Enabled;
    AddEvent(new UserActivatedEvent(Id));
}

// 2. Application service wraps the operation
await _unitOfWork.ExecuteAsync(async ct =>
{
    var user = await _userStore.GetByIdForUpdateAsync(id);
    user.Activate();
}, ct);

// 3. Inside the boundary:
//    - SaveChanges persists the user status change
//    - AggregateEventsCollector finds the user aggregate
//    - CollectEventsAndClear() returns [UserActivatedEvent]
//    - UnitOfWorkExecutor publishes the event via MassTransit
//    - Transaction commits
```

### MassTransit Outbox

`UnitOfWorkExecutor` uses `IPublishEndpoint.Publish` directly. The actual durability guarantee comes from **MassTransit's EF Core Outbox**, which is configured during host startup (`AddEntityFrameworkOutbox<AlcedoDbContext>` with `UseBusOutbox`).

This means:
- Events are written to the `outbox_message` table as part of the same transaction
- A background Quartz job forwards them to the message broker
- If the broker is temporarily unavailable, messages are not lost

---

## Design Principles

1. **One transaction per Unit of Work.** `TransactionManager` ensures at most one active transaction per scope. Nested calls to `BeginTransactionAsync` are idempotent.

2. **Events are collected, not dispatched immediately.** Aggregates accumulate events in memory. They are only published after the action succeeds but before the transaction commits, ensuring atomicity between database state and event stream.

3. **Rollback clears the ChangeTracker.** This prevents detached or modified entities from remaining in the `DbContext` after a failure, which would corrupt subsequent operations in the same request scope.

4. **Application events are immediate.** `PublishApplicationEvent` bypasses the Unit of Work because application events are typically notifications or side-effects that should not fail the main transaction.
