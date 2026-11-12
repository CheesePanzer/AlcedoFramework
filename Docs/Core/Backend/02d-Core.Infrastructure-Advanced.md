# Alcedo.Core.Infrastructure — Advanced Features

This document covers two infrastructure subsystems that operate across aggregate boundaries: **Reference Integrity** (cross-aggregate referential checks) and **Distributed Locks** (Redis-backed reader-writer locks for concurrency control).

---

## Table of Contents

- [Reference Integrity](#reference-integrity)
  - [Why Not Foreign Keys](#why-not-foreign-keys)
  - [Reference Modes](#reference-modes)
  - [Configuration API](#configuration-api)
  - [Checking References](#checking-references)
  - [Scanning References](#scanning-references)
  - [Implementation Overview](#implementation-overview)
- [Distributed Locks](#distributed-locks)
  - [Lock Types](#lock-types)
  - [ApplicationLockProvider API](#applicationlockprovider-api)
  - [Timeout Behavior](#timeout-behavior)
  - [Implementation](#implementation)
- [Design Principles](#design-principles)

---

## Reference Integrity

In a modular monolith with DDD, aggregate roots should not hold foreign keys to other aggregates in a way that creates tight coupling at the database level. However, business rules often require knowing whether an aggregate is referenced before allowing deletion or disabling.

Alcedo's **Reference Integrity** system provides explicit, opt-in cross-aggregate reference checking without database-level foreign keys.

Notably, this integrity tool doesn't provide concurrent manage. Add locks manually!

### Why Not Foreign Keys

- **Module independence:** Modules should not enforce schema constraints on each other's tables
- **Soft deletes:** Business-level "disable" is preferred over physical deletion
- **Snapshot references:** Some references are intentionally decoupled (the child captures data at a point in time)
- **Eventual consistency:** Some relationships are resolved asynchronously, not at the DB level

### Reference Modes

```csharp
[Flags]
public enum ReferenceMode
{
    Strict = 1,      // Full referential integrity check — child must not become orphaned
    Snapshot = 2,    // Decoupled — parent allows child to be deleted/disabled independently
    NoCheck = -1     // No validation; reference exists only for data model purposes
}
```

| Mode | Check on Delete/Disable? | Use Case |
|------|-------------------------|----------|
| `Strict` | Yes | Master-data references (e.g. `Order.Item.ProductId` → `ProductAgg`) |
| `Snapshot` | No | Historical references (e.g. an invoice references a past price list) |
| `NoCheck` | No | Internal navigation-only references within the same aggregate |

### Configuration API

Each aggregate root that is referenced by others defines its relationships via `IAggregateRootReferenceConfiguration<T>`:

Snapshot is to describe a state that the parent is referencing the snapshot of child; 
As a result when checking if a child is referenced, parents will be skipped; 
However, when collecting the aggregate roots that a parent is referencing, the snapshot aggregateroot will be counted in;

```csharp
public class ProductReferenceConfig : IAggregateRootReferenceConfiguration<OrderAgg>
{
    public void Configure(ReferenceEntityBuilder<OrderAgg> builder)
    {
        // OrderItem.ProductId references ProductAgg — strict check
        builder.Through(x => x.Items)
               .References<ProductAgg>(x => x.ProductId, ReferenceMode.Strict);

        // Order.CreatorId references UserAgg — snapshot (don't block user deletion)
        builder.References<UserAgg>(x => x.CreatorId, ReferenceMode.Snapshot);
    }
}
```

**Cross-module references via string identity:**

In a multi-module setup, a contract project may reference an aggregate root from another module without taking a direct assembly dependency on its type. Use the string-identity overload:

```csharp
// "IDENTITY_USR" is the Identity of UserAgg from the Identity module
builder.References("IDENTITY_USR", x => x.CreatorId, ReferenceMode.Snapshot);
```

The identity string format is `{ModuleCode}_{AggCode}` (uppercase), as registered in `AggregateRootMetadataCenter`. At startup, the builder resolves the identity to the actual `Type` via `GetAggregateTypeByIdentity`.

**Fluent API:**

```csharp
// Direct property on the aggregate root — generic (same module)
builder.References<TPrincipal>(fkExpression, mode);

// Direct property on the aggregate root — string identity (cross-module)
builder.References("IDENTITY_USR", fkExpression, mode);

// Navigate through a child entity
builder.Through(x => x.LineItems)
       .References<TPrincipal>(x => x.ProductId, mode);

// Deep navigation: root -> child -> grandchild
builder.Through(x => x.Items)
       .Through(x => x.Details)
       .References<TPrincipal>(x => x.CategoryId, mode);
```

Configurations are scanned automatically at startup from all module assemblies.

### Checking References

Use `IAggRefChecker` to verify whether an aggregate is referenced before deletion or state change:

```csharp
public class DisableProductHandler
{
    private readonly IAggRefChecker _refChecker;

    public async Task Handle(Guid productId, CancellationToken ct)
    {
        var product = await _productStore.GetByIdForUpdateAsync(productId);
        
        // Throws DomErrAggReferenced if any Strict reference exists
        await _refChecker.ThrowIfReferencedAsync<ProductAgg>(productId, ct);
        
        product.Disable();
    }
}
```

| Method | Behavior |
|--------|----------|
| `IsReferencedAsync<T>(id)` | Returns `true` if any `Strict` reference exists |
| `ThrowIfReferencedAsync<T>(id)` | Throws `DomErrAggReferenced` if referenced |

**Important:** For strong consistency under concurrent writes, lock the entity being checked before calling these methods (e.g. via `GetByIdForUpdateAsync`).

### Scanning References

Use `IAggRefScaner` to extract all referenced aggregate IDs along with their types from an aggregate instance.
For example if you have a order bill, you can use this to check the status of related products by collecting their id automatically.
Especially useful in Erp system where a single bill entity can reference multiple types of masterdata;

```csharp
var refs = _refScaner.Scan(order);
// Returns: { [typeof(ProductAgg)]: [guid1, guid2], [typeof(UserAgg)]: [guid3] }
```

The scan traverses the aggregate's object graph recursively (via configured navigation paths) and collects all non-null referenced values (include those are referenced in snapshot mode).

### Implementation Overview

Reference integrity is implemented via expression-tree compilation for performance:

| Component | Responsibility |
|-----------|---------------|
| `ReferenceModelBuilder` / `ReferenceEntityBuilder<T>` | Fluent configuration API |
| `ReferenceMetadataFactory` | Scans assemblies, compiles configuration metadata at startup |
| `ReferenceIntegrityCompiler` | Builds `Expression`-tree delegates for SQL checks and object-graph traversal |
| `ReferenceIntegrityConfigCenter` | Singleton cache of compiled delegates |
| `AggRefChecker` | Executes SQL `AnyAsync` checks against the database |
| `AggRefScaner` | Executes in-memory object-graph scanning |

**Check function compilation:**

For each `Strict` reference, the system compiles a delegate equivalent to:

```csharp
(AlcedoDbContext ctx, Guid id, CancellationToken ct) =>
    ctx.Set<OrderItem>().AnyAsync(e => e.ProductId == id, ct)
```

Multiple foreign keys pointing to the same target within one aggregate are merged into a single `OR` condition.

---

## Distributed Locks

Alcedo provides Redis-backed distributed locks using **Medallion.Threading** with reader-writer semantics. This enables:

- **Write locks (exclusive)** — for mutations that must not race
- **Read locks (shared)** — for concurrent reads that must block writers
- **Batch locking** — acquire multiple locks atomically in a deterministic order to prevent deadlocks

### Lock Types

| Type | Method | Semantics |
|------|--------|-----------|
| **Read lock** | `TryAcquireReadLockAsync` | Shared — multiple readers can hold simultaneously; blocks writers |
| **Write lock** | `TryAcquireWriteLockAsync` | Exclusive — only one writer; blocks all readers and writers |

### ApplicationLockProvider API

Application code uses `ApplicationLockProvider` (registered as scoped) rather than the low-level `IDistributedLockFactory` directly.

```csharp
public class ApplicationLockProvider
{
    public ApplicationLockBuilder CreateLock();
}
```

**Batch locking example:**

```csharp
using var locks = await _lockProvider.CreateLock()
    .AddX<ProductAgg>(productId)          // Exclusive lock, prefix = ProductAgg.Identity
    .AddS<WarehouseAgg>(warehouseIds)     // Shared locks, prefix = WarehouseAgg.Identity
    .AddX("INVENTORY_STOCK", stockId)     // Explicit string identity prefix
    .LockAsync(perLockSecs: 2, totalSecs: 5, ct);

// Critical section — no other writer can modify product or inventory
// Multiple readers can hold warehouse locks concurrently
```

**No-wait mode:**

```csharp
using var locks = await _lockProvider.CreateLock()
    .AddX<ProductAgg>(productId)
    .LockNoWaitAsync(ct);  // Throws DomErrLockConflict immediately if any lock is unavailable
```

`ApplicationLocks` implements `IDisposable`. Disposing releases all held locks in reverse order.

### Timeout Behavior

`LockAsync` accepts two timeout parameters:

| Parameter | Default | Meaning |
|-----------|---------|---------|
| `perLockSecs` | 2 | Max seconds to wait for each individual lock |
| `totalSecs` | 5 | Max total seconds for the entire batch |

The system computes a dynamic timeout per lock that shrinks as the total budget is consumed:

```csharp
// First lock: up to 2 seconds
// Second lock: up to min(2, 5 - elapsed) seconds
// If total budget expires before all locks are acquired → throws DomErrLockConflict
```

### Implementation

| Component | Responsibility |
|-----------|---------------|
| `IDistributedLockFactory` | Low-level read/write lock acquisition per `(id, prefix)` |
| `DistributedLockFactory` | Redis-backed implementation using Medallion.Threading |
| `ApplicationLockFactory` | Batch lock orchestration with deadlock prevention (sorted acquisition) |
| `ApplicationLockBuilder` | Fluent API for constructing batch lock requests |
| `ApplicationLockProvider` | Scoped entry point for application code |
| `LockTimeoutTracker` | Dynamic per-lock timeout budget calculation |

**Lock key format:**

Redis keys are constructed as `DisLock:{identity}:{id}`, where `identity` is resolved from `AggregateRootMetadataCenter`:

- `AddX<TAgg>(id)` → prefix = `AggregateRootMetadataCenter.GetMetaData<TAgg>().Identity`
- `AddX("INVENTORY_STOCK", id)` → prefix = the literal string provided

**Deadlock prevention:** Batch locks are always acquired in a deterministic order — sorted by `prefix`, then by `id`. Duplicate IDs are deduplicated, and if both read and write locks are requested for the same ID, the write lock wins.

---

## Design Principles

1. **Reference integrity is explicit, not implicit.** No database foreign keys across aggregates. Modules declare their reference rules via configuration, and the framework enforces them at the application layer.

2. **Reference checks are compiled, not interpreted.** All SQL checks and object-graph scanners are compiled expression trees at startup. There is no reflection at runtime.

3. **Distributed locks are ordered.** Batch lock acquisition is always sorted to prevent deadlocks. The caller never specifies order — the framework enforces it.

4. **Lock timeouts are budgets, not fixed waits.** The `totalSecs` parameter ensures the entire batch operation does not exceed a predictable latency bound, even when retrying individual locks.
