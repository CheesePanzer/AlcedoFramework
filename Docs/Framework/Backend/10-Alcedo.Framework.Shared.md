# Alcedo.Framework.Shared

This layer sits on top of Alcedo Core and provides a **complete, opinionated base-class stack** for two common data patterns:

- **Reference Data** — static, code-keyed lookup entities (e.g., countries, currencies).
- **Master Data** — business-critical entities with a full lifecycle: state machine, audit log, soft-delete semantics, and optimistic versioning.

Unlike Core, which is intentionally unopinionated, Framework.Shared makes concrete choices about how these entity types behave, how they are persisted, and how they are exposed via HTTP. Modules built on Framework.Shared inherit these conventions automatically.

---

## Table of Contents

- [Domain Layer](#domain-layer)
  - [ReferenceDataAggregateRootBase](#referencedataaggregaterootbase)
  - [MasterDataAggregateRootBase](#masterdataaggregaterootbase)
    - [State Machine](#state-machine)
    - [Audit Log Events](#audit-log-events)
    - [Lifecycle Methods](#lifecycle-methods)
  - [MasterDataAuditLogEntity](#masterdataauditlogentity)
  - [Repository Interfaces](#repository-interfaces)
- [Application Layer](#application-layer)
  - [DTOs](#dtos)
  - [Search Queries & Commands](#search-queries--commands)
  - [Components (Composition)](#components-composition)
    - [ReferenceDataSearchServiceComponent](#referencedatasearchservicecomponent)
    - [MasterDataSearchServiceComponent](#masterdatasearchservicecomponent)
    - [MasterDataEditServiceComponent](#masterdataeditservicecomponent)
    - [MasterDataStatusServiceComponent](#masterdatastatusservicecomponent)
  - [Service Interfaces](#service-interfaces)
  - [Audit Log Services](#audit-log-services)
- [Infrastructure Layer](#infrastructure-layer)
  - [ReferenceDataRepositoryBase](#referencedatarepositorybase)
  - [MasterDataRepositoryBase](#masterdatarepositorybase)
  - [MasterDataLogStoreBase](#masterdatalogstorebase)
  - [Configuration Bases](#configuration-bases)
- [Presentation Layer](#presentation-layer)
  - [ReferenceDataApiControllerBase](#referencedataapicontrollerbase)
  - [MasterDataControllerBase](#masterdatacontrollerbase)
- [Consumer Layer](#consumer-layer)
  - [MasterDataAuditLogConsumerBase](#masterdataauditlogconsumerbase)
- [Design Summary](#design-summary)

---

## Domain Layer

### ReferenceDataAggregateRootBase

The simplest aggregate in the Framework. It extends `AggregateRoot` and adds a single business key:

```csharp
public abstract class ReferenceDataAggregateRootBase : AggregateRoot
{
    public string Code { get; protected set; }
}
```

`Code` is treated as the natural, human-readable identifier (e.g., `"USD"`, `"CN"`). The infrastructure base ensures it is unique at the database level.

---

### MasterDataAggregateRootBase

Inherits from `ReferenceDataAggregateRootBase` and adds the full master-data lifecycle:

```csharp
public abstract class MasterDataAggregateRootBase : ReferenceDataAggregateRootBase
{
    public string Name { get; protected set; }
    public MasterDataStateValue Status { get; protected set; }

    public Guid CreatorId { get; protected set; }
    public string CreatorSnap { get; protected set; }
    public Guid UpdatorId { get; protected set; }
    public string UpdatorSnap { get; protected set; }

    protected virtual StateFlowBase<MasterDataStateValue, MasterDataStateTransitionTrigger> StatusFlow
        => new MasterDataDefaultStatusFlow();

    protected virtual StateActionGuardBase<MasterDataStateValue, MasterDataStateAction> ActionGuard
        => new MasterDataDefaultOperationGuard();
}
```

Key responsibilities:
- **State management** — transitions are driven by `StatusFlow`; illegal transitions throw `DomErrStatusInvalid`.
- **Action guarding** — side-effect actions (update, delete) are validated by `ActionGuard`; illegal actions throw `DomErrStatusActionInvalid`.
- **Audit trail** — every transition and guarded action emits a `MasterDataAuditLogEvent` containing operator snapshot, before/after status, and an optional note.
- **Optimistic concurrency** — inherits `Version` from `AggregateRoot`; update commands carry the expected version.

#### State Machine

The default flow is:

```
Transient --Create--> Draft --Enable--> Enabled <---> Disabled
                                              Disable    Enable
```

Defined by `MasterDataDefaultStatusFlow`, which splits its rules into `Base` and `Additional`. A concrete aggregate can override `Additional` to inject extra transitions without replacing the default graph.

Two built-in action guards are provided:
- `MasterDataDefaultOperationGuard` — allows Update in Draft/Enabled/Disabled; allows Delete in Draft/Disabled.
- `MasterDataArchiveOperationGuard` — adds an `Archive` action allowed only from Disabled.

Concrete aggregates override `ActionGuard` to swap or extend behavior.

#### Audit Log Events

Every status change or guarded action calls `RecordAudit`, which builds a `MasterDataAuditLogEvent` via the abstract factory `CreateAuditLogEventInternal`. This keeps the event type concrete (so MassTransit can route it to the correct consumer) while the base class enforces that an event is always raised.

#### Lifecycle Methods

Public surface exposed to the application layer:

```csharp
public virtual void Enable(Guid operId, string operSnap);
public virtual void Disable(Guid operId, string operSnap);
public virtual void Delete(Guid operId, string operSnap);
```

`Update` is protected; the aggregate exposes its own `UpdateXxx` method that internally calls `base.Update(name, operId, operSnap)` so the base class can record the action guard check.

---

### MasterDataAuditLogEntity

The write-side entity that stores the audit trail. It is **not** an aggregate root; it is a plain `EntityBase` owned by a separate store/repository:

```csharp
public abstract class MasterDataAuditLogEntity : EntityBase
{
    public Guid EntityId { get; init; }
    public string OperationName { get; init; }
    public string FromStatusName { get; init; }
    public string ToStatusName { get; init; }
    public DateTime OperatedAt { get; init; }
    public Guid OperatorId { get; init; }
    public string OperatorSnap { get; init; }
    public string? Note { get; init; }
}
```

---

### Repository Interfaces

```csharp
public interface IReferenceDataRepositoryBase<T> : IAggregateRootRepositoryBase<T>
    where T : ReferenceDataAggregateRootBase
{
    Task<bool> CheckIfCodeExistsAsync(string code, CancellationToken ct = default);
}

public interface IMasterDataRepositoryBase<T> : IReferenceDataRepositoryBase<T>
    where T : MasterDataAggregateRootBase
{
    // Read (NoTracking)
    Task<T?> GetEnabledByIdNoTrackingAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetEnabledListByIdNoTrackingAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    // Read (Tracking)
    Task<T?> GetEnabledByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<T>> GetEnabledListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    // Pessimistic Locking
    Task<T?> GetEnabledByIdForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<T?> GetEnabledByIdForShareAsync(Guid id, CancellationToken ct = default);
    Task<List<T>> GetEnabledListPartialByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetEnabledListPartialByIdsForShareAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<List<T>> GetEnabledListStrictByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetEnabledListStrictByIdsForShareAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    // Validation
    Task<bool> CheckIdIfEnabledAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> CheckIdsIfEnabledAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
}
```

All `Enabled*` methods filter by `Status == MasterDataStates.Enabled`. The `Partial` variants return whatever exists; the `Strict` variants throw `DomErrAggNotFound` if any ID is missing or not enabled. This gives callers an explicit choice between tolerance and strictness.

---

## Application Layer

### DTOs

```csharp
public abstract record ReferenceDataDtoBase : AggregateRootDtoBase
{
    public required string Code { get; set; }
}

public abstract record MasterDataDtoBase : ReferenceDataDtoBase
{
    public required string Name { get; set; }
    public required MasterDataStateValue Status { get; set; }
    public required Guid CreatorId { get; set; }
    public required string CreatorSnap { get; set; }
    public required Guid UpdatorId { get; set; }
    public required string UpdatorSnap { get; set; }
}

public abstract record MasterDataAuditLogDto : EntityDtoBase
{
    public required Guid EntityId { get; set; }
    public required string OperationName { get; set; }
    public required string FromStatusName { get; set; }
    public required string ToStatusName { get; set; }
    public required DateTime OperatedAt { get; set; }
    public required Guid OperatorId { get; set; }
    public required string OperatorSnap { get; set; }
    public required string? Note { get; set; }
}
```

---

### Search Queries & Commands

```csharp
public abstract record ReferenceDataSearchQueryBase : AggregateRootSearchQueryBase
{
    public string? Code { get; set; }
}

public abstract record MasterDataSearchQueryBase : ReferenceDataSearchQueryBase
{
    public string? Name { get; set; }
    public MasterDataStateValue? Status { get; set; }
}

public abstract record MasterDataCreateCommandBase
{
    public required string Code { get; set; }
    public required string Name { get; set; }
}

public abstract record MasterDataUpdateCommandBase
{
    public required int Version { get; set; }
    public required string Name { get; set; }
}
```

Commands are intentionally minimal. The application layer maps them to aggregate method calls; the aggregate decides what else changes (status, audit fields, etc.).

---

### Components (Composition)

Framework.Shared follows the Core Application pattern of **composition over inheritance**. A concrete master-data application service is not required to inherit a massive base class; instead, it composes small, focused components.

#### ReferenceDataSearchServiceComponent

```csharp
public abstract class ReferenceDataSearchServiceComponent<TDto, TQuery>
    : AggregateRootSearchServiceComponent<TDto, TQuery>
    where TDto : ReferenceDataDtoBase
    where TQuery : ReferenceDataSearchQueryBase
```

Thin wrapper that pins the DTO and query types to the ReferenceData hierarchy.

#### MasterDataSearchServiceComponent

```csharp
public abstract class MasterDataSearchServiceComponent<TDto, TQuery>
    : ReferenceDataSearchServiceComponent<TDto, TQuery>
    where TDto : MasterDataDtoBase
    where TQuery : MasterDataSearchQueryBase
```

Further narrows the types for master data.

#### MasterDataEditServiceComponent

```csharp
public abstract class MasterDataEditServiceComponent<TAgg, TCreateCommand, TUpdateCommand>
    where TAgg : MasterDataAggregateRootBase
```

Encapsulates create/update/delete with three cross-cutting concerns:
- **Distributed locking** — `ApplicationLockProvider` acquires an exclusive lock on the aggregate ID before mutation.
- **Reference integrity** — `IAggRefChecker` blocks deletion if the aggregate is referenced by others.
- **Unit of work** — `IUnitOfWorkExecutor` wraps the entire mutation in a transaction + outbox publish.

`CreateAsync` checks code uniqueness. `UpdateByIdAsync` validates the aggregate version. `DeleteByIdAsync` checks references, then removes the aggregate.

Both `CreateAggreageteInternal` and `UpdateAggreagateInternal` are abstract, forcing the concrete component to map commands to aggregate constructors/methods.

#### MasterDataStatusServiceComponent

```csharp
public abstract class MasterDataStatusServiceComponent<TAgg>
    where TAgg : MasterDataAggregateRootBase
```

Encapsulates `Enable` and `Disable` transitions, plus `Delete`. All three acquire an exclusive distributed lock, load the aggregate with `FOR UPDATE`, and execute inside `IUnitOfWorkExecutor`.

`DeleteByIdAsync` also checks reference integrity before allowing removal.

---

### Service Interfaces

```csharp
public interface IReferenceDataApplicationService<TDto, in TSearch>
    where TDto : ReferenceDataDtoBase
    where TSearch : ReferenceDataSearchQueryBase
{
    Task<PagedResult<TDto>> GetPagedDtosAsync(TSearch query, SortQuery sortQuery, PaginationQuery pagiQuery, CancellationToken ct = default);
    Task<TDto> GetDtoByIdAsync(Guid id, CancellationToken ct = default);
}

public interface IMasterDataApplicationService<TDto, in TSearch, in TCreateCommand, in TUpdateCommand>
    : IReferenceDataApplicationService<TDto, TSearch>
    where TDto : MasterDataDtoBase
    where TSearch : MasterDataSearchQueryBase
    where TCreateCommand : MasterDataCreateCommandBase
    where TUpdateCommand : MasterDataUpdateCommandBase
{
    Task<Guid> CreateAsync(TCreateCommand command, Guid operId, CancellationToken ct = default);
    Task UpdateByIdAsync(Guid id, TUpdateCommand command, Guid operId, CancellationToken ct = default);
    Task DeleteByIdAsync(Guid id, Guid operId, CancellationToken ct = default);
    Task EnableByIdAsync(Guid id, Guid operId, CancellationToken ct = default);
    Task DisableByIdAsync(Guid id, Guid operId, CancellationToken ct = default);
}
```

A concrete application service implements `IMasterDataApplicationService` and delegates to the components above. This keeps the service class thin: it is essentially a facade wiring together search, edit, and status components.

---

### Audit Log Services

```csharp
public interface IMasterDataLogWriteService<in TLogEvt>
    where TLogEvt : MasterDataAuditLogEvent
{
    Task SaveLogFromEvtAsync(TLogEvt evt, CancellationToken ct = default);
}

public abstract class MasterDataLogWriteServiceBase<TLogEntity, TLogEvt> : IMasterDataLogWriteService<TLogEvt>
    where TLogEntity : MasterDataAuditLogEntity
    where TLogEvt : MasterDataAuditLogEvent

public interface IMasterDataLogSearchService<TDto>
    where TDto : MasterDataAuditLogDto
{
    Task<PagedResult<TDto>> GetPagedDtosAsync(MasterDataLogSearchQuery query, SortQuery sortQuery, PaginationQuery pagiQuery, CancellationToken ct = default);
}
```

The write side is consumed by the MassTransit consumer (see below). The search side is exposed through the controller for UI audit-log views.

---

## Infrastructure Layer

### ReferenceDataRepositoryBase

```csharp
public abstract class ReferenceDataRepositoryBase<T>
    : AggregateRootRepositoryBase<T>, IReferenceDataRepositoryBase<T>
    where T : ReferenceDataAggregateRootBase
{
    public virtual async Task<bool> CheckIfCodeExistsAsync(string code, CancellationToken ct = default)
        => await DbSet.AnyAsync(x => x.Code == code, ct);
}
```

Minimal addition to Core's `AggregateRootRepositoryBase`: code uniqueness check.

---

### MasterDataRepositoryBase

```csharp
public abstract class MasterDataRepositoryBase<T>
    : ReferenceDataRepositoryBase<T>, IMasterDataRepositoryBase<T>
    where T : MasterDataAggregateRootBase
```

Implements the full `IMasterDataRepositoryBase<T>` contract:
- Standard queries filter by `Status == MasterDataStates.Enabled`.
- No-tracking queries use `AsNoTracking()`.
- Pessimistic-lock queries chain through `PessimisticLockQueryBuilder` (`QueryWithPessimisticLock` / `QueryListWithPessimisticLock`) and append a raw SQL condition on `status`.
- `Strict` variants compare returned IDs against the requested set and throw `DomErrAggNotFound` for any missing items.

This base eliminates repetitive "Enabled + locking" query logic from every master-data module.

---

### MasterDataLogStoreBase

```csharp
public abstract class MasterDataLogStoreBase<TLog>
    : EntityStoreBase<TLog>, IMasterDataLogStoreBase<TLog>
    where TLog : MasterDataAuditLogEntity
```

Thin wrapper over `EntityStoreBase<T>` for audit-log entities. No additional behavior; it exists only to satisfy the domain contract.

---

### Configuration Bases

```csharp
public abstract class ReferenceDataConfigurationBase<T> : AggregateRootConfigurationBase<T>
    where T : ReferenceDataAggregateRootBase
{
    protected override void ConfigureIndexes(EntityTypeBuilder<T> builder)
    {
        base.ConfigureIndexes(builder);
        builder.HasIndex(e => e.Code).IsUnique();
    }
}

public abstract class MasterDataConfigurationBase<T> : ReferenceDataConfigurationBase<T>
    where T : MasterDataAggregateRootBase
{
    protected override void ConfigureConverters(EntityTypeBuilder<T> builder)
    {
        base.ConfigureConverters(builder);
        builder.Property(e => e.Status)
            .HasConversion<ValueAtomConverter<MasterDataStateValue>>()
            .HasMaxLength(50);
    }
}

public class MasterDataLogConfigurationBase<T> : EntityConfigurationBase<T>
    where T : MasterDataAuditLogEntity
{
    protected override void ConfigureProperties(EntityTypeBuilder<T> builder)
    {
        base.ConfigureProperties(builder);
        builder.Property(e => e.Note).HasColumnType("text");
    }
}
```

- `ReferenceDataConfigurationBase` enforces a unique index on `Code`.
- `MasterDataConfigurationBase` adds `ValueAtomConverter<MasterDataStateValue>` for the status column.
- `MasterDataLogConfigurationBase` maps `Note` to `text` for unbounded length.

---

## Presentation Layer

### ReferenceDataApiControllerBase

```csharp
public abstract class ReferenceDataApiControllerBase<TDto, TSearch> : AlcedoApiAreaControllerBase
    where TDto : ReferenceDataDtoBase
    where TSearch : ReferenceDataSearchQueryBase
{
    [HttpGet]
    [Operation("PageView", "View Items' List")]
    public virtual async Task<ActionResult<PagedResult<TDto>>> GetPagedItems(...)

    [HttpGet("{id}")]
    [Operation("DetailView", "View Item's Detail")]
    public virtual async Task<ActionResult<TDto>> GetItemInfo(Guid id, ...)
}
```

Provides the two read endpoints every reference-data controller needs: paged list and single-item detail. Inherits from `AlcedoApiAreaControllerBase` so API versioning and `[Operation]` metadata are already wired.

---

### MasterDataControllerBase

```csharp
public abstract class MasterDataControllerBase<TDto, TSearch, TCreateCommand, TUpdateCommand, TLogDto>
    : ReferenceDataApiControllerBase<TDto, TSearch>
    where TDto : MasterDataDtoBase
    where TSearch : MasterDataSearchQueryBase
    where TCreateCommand : MasterDataCreateCommandBase
    where TUpdateCommand : MasterDataUpdateCommandBase
    where TLogDto : MasterDataAuditLogDto
```

Extends the read surface with the full master-data command surface:

| Method | Route | Operation |
|--------|-------|-----------|
| `POST` | `/` | Create |
| `PUT` | `/{id}` | Update |
| `PATCH` | `/{id}/status:enable` | Enable |
| `PATCH` | `/{id}/status:disable` | Disable |
| `DELETE` | `/{id}` | Delete |
| `GET` | `/logs` | ViewAuditLogs |

All write endpoints inject `CurrentUser.Id` as the operator ID. The audit-log endpoint delegates to `IMasterDataLogSearchService<TLogDto>`.

---

## Consumer Layer

### MasterDataAuditLogConsumerBase

```csharp
public abstract class MasterDataAuditLogConsumerBase<TEvt> : IConsumer<TEvt>
    where TEvt : MasterDataAuditLogEvent
{
    private readonly IMasterDataLogWriteService<TEvt> _logService;

    public async Task Consume(ConsumeContext<TEvt> context)
    {
        await _logService.SaveLogFromEvtAsync(context.Message);
    }
}
```

A thin, generic consumer that receives the audit-log event published by the aggregate and persists it via `IMasterDataLogWriteService`. Because the event type is abstract, each concrete master-data module defines its own event subclass and registers a corresponding consumer subclass. MassTransit routes the concrete event to the correct consumer automatically.

This keeps audit-log persistence asynchronous and decoupled from the main transaction: the event travels through the outbox, so the log write is eventually consistent but reliably delivered.

---

## Design Summary

Framework.Shared demonstrates how Core's primitives are composed into a **productivity layer**:

1. **Convention over configuration** — A new master-data entity needs only a concrete aggregate, a few DTOs, and a controller. Repository, state machine, audit log, locking, and reference-integrity checks are inherited or composed.

2. **Hierarchical specialization** — `AggregateRoot` -> `ReferenceDataAggregateRootBase` -> `MasterDataAggregateRootBase`. Each level adds semantics without breaking the one below.

3. **Composition in Application** — The service layer is built from `SearchComponent + EditComponent + StatusComponent`, not from a 500-line base class. Each component has a single responsibility.

4. **Locking + integrity by default** — Edit and status operations acquire distributed locks and check reference integrity automatically. A developer cannot accidentally forget these protections.

5. **Audit as event-driven** — Audit logs are domain events emitted by the aggregate, consumed asynchronously, and stored in a separate table. This keeps the aggregate pure while guaranteeing an immutable history.
