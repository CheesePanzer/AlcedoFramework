# Alcedo.Core.Domain

The Domain layer defines the entity model, repository contracts, domain events, state machines, and the exception hierarchy for business rule violations. It depends only on `Alcedo.Core.Abstractions` and has no knowledge of persistence, messaging, or HTTP.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Entity Model](#entity-model)
  - [EntityBase](#entitybase)
  - [AggregateRoot](#aggregateroot)
- [Repository Contracts](#repository-contracts)
- [Domain Events](#domain-events)
- [Domain Exceptions](#domain-exceptions)
- [AggregateRootMetadataCenter](#aggregaterootmetadatacenter)
- [Reference Integrity Contracts](#reference-integrity-contracts)

---

## Architecture Overview

| Concept | Location | Purpose |
|---------|----------|---------|
| `EntityBase` | `Entity/` | Base for all domain entities with UUID v7 primary key |
| `AggregateRoot` | `Entity/` | Base for aggregate roots; adds versioning, audit timestamps, domain events |
| `IEntityStoreBase<T>` | `IRepositoryBase/` | Write-side contract (loaded by Infrastructure) |
| `IAggregateRootRepositoryBase<T>` | `IRepositoryBase/` | Marker interface for aggregate root stores |
| `DomainEventBase` | `Event/` | Base for all domain events |
| `IDomainEventPublisher` | `IDomainEventPublisher.cs` | Contract for publishing domain events outside UoW |
| State Machine | `StateMachine/` | State transitions and action guards (see `03a` document) |
| Exceptions | `DomException/` | Business rule violation exceptions mapping to HTTP 400/409 |
| Reference Integrity | `ReferenceIntegrity/` | Cross-aggregate reference checking contracts |

---

## Entity Model

### EntityBase

The root of all domain entities:

```csharp
public class EntityBase
{
    public Guid Id { get; protected set; } = Uuid.NewDatabaseFriendly(Database.PostgreSql);
}
```

- Uses **UUID v7** (via `UUIDNext`) — time-sortable, database-friendly, no central coordination needed
- `Id` is assigned at construction time, never changed
- All properties use `protected set` to ensure mutations happen through domain methods

### AggregateRoot

Specialization of `EntityBase` for aggregate roots. Adds:

| Member | Purpose |
|--------|---------|
| `Version` | Optimistic concurrency token (auto-managed by `AggregateSaveChangesInterceptor`) |
| `CreatedAt` / `UpdatedAt` | Audit timestamps (auto-managed) |
| `ForceModified` | Forces EF Core to treat an unchanged aggregate as modified |
| `_events` | Internal queue of domain events |

**Domain event management:**

```csharp
protected void AddEvent(DomainEventBase @event);           // Append event
public ICollection<DomainEventBase> CollectEventsAndClear(); // Drain queue (called by UoW)
```

**Lifecycle hooks (called by interceptor):**

```csharp
public void TouchForCreate();   // Sets Version = 1, CreatedAt, UpdatedAt
public void TouchForUpdate();   // Increments Version, updates UpdatedAt
public void MarkAsModified();   // Sets ForceModified = true
public void ResetModified();    // Clears ForceModified
```

---

## Repository Contracts

The Domain layer defines what the Infrastructure layer must implement. This inverts the dependency: Domain knows **what** it needs, Infrastructure decides **how** to provide it.

### IEntityStoreBase<TEntity>

Write-side contract for all entities:

```csharp
public interface IEntityStoreBase<TEntity> where TEntity : EntityBase
{
    // Single entity loading
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TEntity?> GetByIdNoTrackingAsync(Guid id, CancellationToken ct = default);
    Task<TEntity?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<TEntity?> GetByIdForShareAsync(Guid id, CancellationToken ct = default);

    // Batch loading
    Task<IReadOnlyList<TEntity>> GetListByIdsNoTrackingAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<List<TEntity>> GetListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<List<TEntity>> GetListPartialByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> GetListPartialByIdsForShareAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<List<TEntity>> GetListStrictByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> GetListStrictByIdsForShareAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    // Mutation
    void Add(TEntity entity);
    void AddRange(IEnumerable<TEntity> entities);
    void Remove(TEntity entity);
    void RemoveRange(IEnumerable<TEntity> entities);
}
```

| Suffix | Meaning |
|--------|---------|
| `NoTracking` | Read-only, no EF change tracking |
| `ForUpdate` | Pessimistic write lock (`FOR UPDATE`) |
| `ForShare` | Pessimistic read lock (`FOR SHARE`) |
| `Partial` | Returns whatever is found (no error if some IDs missing) |
| `Strict` | Throws `DomErrAggNotFound` if any ID is missing |

### IAggregateRootRepositoryBase<T>

A marker interface with no additional members:

```csharp
public interface IAggregateRootRepositoryBase<T> : IEntityStoreBase<T> where T : AggregateRoot { }
```

It exists purely for type-level distinction — application code can depend on `IAggregateRootRepositoryBase<UserAgg>` to signal that only aggregate roots are accepted.

---

## Domain Events

### DomainEventBase

```csharp
public abstract record DomainEventBase(Guid AggregateId) : EventRoot;
```

All domain events carry:
- `AggregateId` — the ID of the aggregate that raised the event
- `EventId` — unique event identifier (from `EventRoot`)
- `Timestamp` — UTC time of creation (from `EventRoot`)

### Publishing

Within a Unit of Work, events are collected automatically and published at commit time. Outside a Unit of Work, use `IDomainEventPublisher`:

```csharp
public interface IDomainEventPublisher
{
    Task Publish(DomainEventBase domainEvent, CancellationToken ct = default);
}
```

**Example:**

```csharp
// Inside aggregate — just add to queue
protected void OnUserCreated()
{
    AddEvent(new UserCreatedEvent(Id));
}

// Outside UoW — publish directly
await _domainEventPublisher.Publish(new UserCacheRefreshEvent(userId), ct);
```

---

## Domain Exceptions

All domain exceptions derive from `AlcedoException` (defined in Abstractions) and map to specific HTTP status codes.

| Exception | HTTP Status | Scenario |
|-----------|-------------|----------|
| `DomErrResourceNotFound` / `DomErrResourceNotFound<TEntity>` | 404 | Aggregate not found by ID |
| `DomErrAggReferenced` | 409 | Aggregate is referenced by others and cannot be removed |
| `DomErrAggVersionConflict` / `DomErrAggVersionConflict<TAgg>` | 409 | Optimistic concurrency version mismatch |
| `DomErrMultipleFound` | 409 | Expected one result, found multiple |
| `DomErrRuleViolation` | 400 | Business rule violated |
| `DomErrStatusInvalid` | 400 | Invalid status transition |
| `DomErrStatusActionInvalid` | 400 | Action not allowed in current status |
| `DomErrDuplicateEntry` | 409 | Unique constraint violation |
| `DomErrLockConflict` | 409 | Resource locked by concurrent operation |

**Naming convention:** `DomErr{Name}` — always prefix with `DomErr` to distinguish from application-layer exceptions (`AppErr*`).

---

## AggregateRootMetadataCenter

A startup-time registry that scans all module assemblies for `AggregateRoot` subclasses and extracts metadata from the `[AggregateRootDescription]` attribute. Each aggregate root must declare a unique code and name:

```csharp
[AggregateRootDescription(AggCode: "USR", AggName: "User")]
internal sealed class UserAgg : MasterDataAggregateRootBase { }
```

The module itself provides `ModuleCode` and `ModuleName`. At startup, `AggregateRootMetadataCenter` combines these into two identifiers:

| Property | Format | Example |
|----------|--------|---------|
| `Identity` | `{ModuleCode}_{AggCode}` (uppercase) | `IDENTITY_USR` |
| `FullName` | `{ModuleName}.{AggName}` | `Identity.User` |

**Lookup by type:**

```csharp
var meta = AggregateRootMetadataCenter.GetMetaData<UserAgg>();
// meta.Identity  => "IDENTITY_USR"
// meta.FullName  => "Identity.User"
// meta.Code      => "USR"
// meta.Name      => "User"
// meta.ModuleCode => "IDENTITY"
// meta.ModuleName => "Identity"
```

**Lookup by string identity (cross-module):**

```csharp
var type = AggregateRootMetadataCenter.GetAggregateTypeByIdentity("IDENTITY_USR");
```

This string-based lookup is used when a module's contract project references an aggregate root from another module without taking a direct type dependency.

This metadata is used by:
- Distributed locks (key prefix generation via `Identity`)
- Reference integrity (string-identity references across modules)
- Logging and diagnostics

Initialization happens automatically during host startup via `AddAlcedoCoreDomain`.

---

## Reference Integrity Contracts

The Domain layer defines the contracts for cross-aggregate reference checking (see `02d` for implementation details):

```csharp
public interface IAggRefChecker
{
    Task<bool> IsReferencedAsync<T>(Guid id, CancellationToken ct = default) where T : AggregateRoot;
    Task ThrowIfReferencedAsync<T>(Guid id, CancellationToken ct = default) where T : AggregateRoot;
}

public interface IAggRefScaner
{
    IReadOnlyDictionary<Type, IReadOnlyCollection<Guid>> Scan<T>(T aggregate) where T : AggregateRoot;
}
```

Configuration is done via `IAggregateRootReferenceConfiguration<T>` and the fluent `ReferenceEntityBuilder<T>` API.
