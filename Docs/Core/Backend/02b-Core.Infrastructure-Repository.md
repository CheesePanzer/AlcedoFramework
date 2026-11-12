# Alcedo.Core.Infrastructure — Repository Layer

This document covers the data access layer of Alcedo Core, implementing CQRS (Command Query Responsibility Segregation) with separate write and read repositories.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Write Side — EntityStoreBase](#write-side--entitystorebase)
  - [Why "Store" instead of "Repository"](#why-store-instead-of-repository)
  - [Loading by ID](#loading-by-id)
  - [Batch Loading](#batch-loading)
  - [Pessimistic Locking](#pessimistic-locking)
  - [Add / Remove](#add--remove)
- [Write Side — AggregateRepositoryBase](#write-side--aggregaterepositorybase)
- [Read Side — EntityQueryRepositoryBase](#read-side--entityqueryrepositorybase)
  - [Projection with Mapster](#projection-with-mapster)
  - [Search and Pagination](#search-and-pagination)
- [SearchQueryBuilder](#searchquerybuilder)
  - [BuildFilter](#buildfilter)
  - [BuildOrderBy](#buildorderby)
  - [Caching](#caching)
- [PessimisticLockQueryBuilder](#pessimisticlockquerybuilder)
- [Design Principles](#design-principles)

---

## Architecture Overview

Alcedo separates write and read paths:

| Side | Base Class | Interface | Purpose |
|------|-----------|-----------|---------|
| **Write** | `EntityStoreBase<TEntity>` | `IEntityStoreBase<TEntity>` | Load, add, remove entities. Tracks changes via EF Core. |
| **Write (Aggregate)** | `AggregateRepositoryBase<T>` | `IAggregateRootRepositoryBase<T>` | Same as above, but constrained to `AggregateRoot`. |
| **Read** | `EntityQueryRepositoryBase<TEntity, TDto, TQuery>` | `IEntityQueryRepository<TDto, TQuery>` | Query data, project to DTOs. No tracking. |

**Key rule:** Write repositories load **entities** (for modification). Read repositories return **DTOs** (for display). They do not mix.

---

## Write Side — EntityStoreBase

`EntityStoreBase<TEntity>` is the base class for all write-side data access where `TEntity : EntityBase`. It is called a **Store** rather than a Repository to signal its narrower scope: it handles persistence of single entities and small batches by ID, not complex queries.

```csharp
public abstract class EntityStoreBase<TEntity> : IEntityStoreBase<TEntity>
    where TEntity : EntityBase
{
    protected readonly AlcedoDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    public EntityStoreBase(AlcedoDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }
}
```

### Why "Store" instead of "Repository"

In Alcedo, the term **Store** is reserved for write-side classes that:
- Load entities by identifier (for modification)
- Apply pessimistic locks
- Add or remove entities

The term **Repository** is used for:
- Read-side query classes (returning DTOs)
- Framework-level repository abstractions (e.g. `IMasterDataRepositoryBase`)

This distinction reinforces the CQRS boundary.

### Loading by ID

| Method | Tracking | Lock | Returns |
|--------|----------|------|---------|
| `GetByIdAsync(id)` | Yes | None | Single entity or null |
| `GetByIdNoTrackingAsync(id)` | No | None | Single entity or null (read-only) |
| `GetByIdForUpdateAsync(id)` | Yes | `FOR UPDATE` | Single entity or null |
| `GetByIdForShareAsync(id)` | Yes | `FOR SHARE` | Single entity or null |

```csharp
// Load for modification
var user = await _userStore.GetByIdAsync(userId);
user.Update("New Name", operId, operSnap);

// Load with pessimistic write lock
var user = await _userStore.GetByIdForUpdateAsync(userId);
user.Lock(operId, operSnap);
```

`GetByIdNoTrackingAsync` is useful when the write path needs to inspect another entity without modifying it.

### Batch Loading

| Method | Tracking | Lock | Strictness |
|--------|----------|------|------------|
| `GetListByIdsAsync(ids)` | Yes | None | Returns whatever is found |
| `GetListByIdsNoTrackingAsync(ids)` | No | None | Returns whatever is found |
| `GetListPartialByIdsForUpdateAsync(ids)` | Yes | `FOR UPDATE` | Returns whatever is found |
| `GetListStrictByIdsForUpdateAsync(ids)` | Yes | `FOR UPDATE` | Throws `DomErrAggNotFound` if any ID missing |

```csharp
// Strict batch load — throws if any user is missing
var users = await _userStore.GetListStrictByIdsForUpdateAsync(userIds);
```

### Add / Remove

```csharp
public void Add(TEntity entity);
public void AddRange(IEnumerable<TEntity> entities);
public void Remove(TEntity entity);
public void RemoveRange(IEnumerable<TEntity> entities);
```

**No `SaveChanges` here.** The Store only manipulates the `DbSet`. Persistence happens at the Unit of Work boundary (see `UnitOfWorkExecutor`).

---

## Write Side — AggregateRepositoryBase

A thin specialization of `EntityStoreBase` for aggregate roots:

```csharp
public abstract class AggregateRepositoryBase<T> : EntityStoreBase<T>, IAggregateRootRepositoryBase<T>
    where T : AggregateRoot
{
    public AggregateRepositoryBase(AlcedoDbContext context) : base(context) { }
}
```

It exists purely as a type-level distinction. In practice, most domain repositories inherit from `AggregateRepositoryBase` or a framework-level variant like `MasterDataRepositoryBase`.

**Example:**

```csharp
[RegisterAs(typeof(IUserRepository))]
internal class UserRepository : AggregateRepositoryBase<UserAgg>, IUserRepository
{
    public UserRepository(AlcedoDbContext context) : base(context) { }

    public async Task<bool> CheckIfLoginNameExistsAsync(string loginName, CancellationToken ct = default)
    {
        return await DbSet.AnyAsync(x => x.LoginName == loginName, ct);
    }
}
```

---

## Read Side — EntityQueryRepositoryBase

The read-side repository base. It queries entities and projects them to DTOs using **Mapster**. No entity instances are returned.

```csharp
public abstract class EntityQueryRepositoryBase<TEntity, TDto, TQuery> : IEntityQueryRepository<TDto, TQuery>
    where TEntity : EntityBase
    where TDto : EntityDtoBase
    where TQuery : EntitySearchQueryBase
{
    protected readonly AlcedoDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected EntityQueryRepositoryBase(AlcedoDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }
}
```

### Projection with Mapster

```csharp
protected virtual IQueryable<TDto> Project(IQueryable<TEntity> query)
    => query.ProjectToType<TDto>();
```

By default, uses Mapster's `ProjectToType` for efficient server-side projection. Override this method if you need custom mapping logic.

### Search and Pagination

```csharp
public virtual async Task<(IReadOnlyList<TDto> Items, int TotalCount)> GetPagedAsync(
    TQuery query, PaginationQuery pagiQuery, SortQuery sortQuery, CancellationToken ct = default);

public virtual async Task<IReadOnlyList<TDto>> GetListAsync(
    TQuery query, SortQuery sortQuery, CancellationToken ct = default);

public virtual async Task<TDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
```

**Example:**

```csharp
[RegisterAs(typeof(IUserQueryRepository))]
internal class UserQueryRepository : EntityQueryRepositoryBase<UserAgg, UserDto, UserSearchQuery>, IUserQueryRepository
{
    public UserQueryRepository(AlcedoDbContext context) : base(context) { }
}
```

**Key differences from write side:**
- `AsNoTracking()` is always used
- Returns DTOs, never entities
- Search query is built via `SearchQueryBuilder`
- Sorting uses `SearchQueryBuilder.BuildOrderByOrDefault`

---

## SearchQueryBuilder

`SearchQueryBuilder` dynamically constructs `IQueryable<TEntity>` filter and order-by expressions from a search query DTO. It uses reflection once per `(EntityType, QueryType)` pair, caching the results in `SearchQueryMetaCache` for subsequent reuse.

### BuildFilter

```csharp
public static Func<IQueryable<TEntity>, IQueryable<TEntity>> BuildFilter<TEntity, TSearch>(TSearch query);

// With custom predicates for conditions the attribute system cannot express
public static Func<IQueryable<TEntity>, IQueryable<TEntity>> BuildFilter<TEntity, TSearch>(
    TSearch query,
    params Expression<Func<TEntity, bool>>[] customPredicates);
```

**Supported filter types:**

| Query Property Type | FilterKind | SQL Generated |
|---------------------|------------|---------------|
| `IReadOnlyList<Guid>?` | `GuidList` | `WHERE Id IN (...)` |
| `IReadOnlyList<int>?` | `IntList` | `WHERE Field IN (...)` |
| `IReadOnlyList<string>?` | `StringList` | `WHERE Field IN (...)` |
| `IReadOnlyList<ValueAtom>?` | `ValueAtomList` | `WHERE Field IN (...)` |
| `string?` | `String` | `=` or `LIKE` (depends on `StringSearchMode`) |
| `int?`, `long?`, `decimal?`, `double?`, `bool?` | `Int`, `Long`, `Decimal`, `Double`, `Bool` | `=` |
| `Guid?` | `Guid` | `=` |
| `Enum?` | `Enum` | `=` (cast to int) |
| `ValueAtom?` | `ValueAtom` | `=` |
| `DateTime?` + `[DateRangeFilter]` | `DateRange` | `>=` (start) or `<` (end) |

**Null handling:** All nullable properties are skipped when `null`. Non-null values are applied as filter conditions.

**String search modes:**

```csharp
public enum StringSearchMode
{
    Exact,      // = value
    StartsWith, // LIKE 'value%'  (default)
    EndsWith,   // LIKE '%value'
    Contains    // LIKE '%value%'
}
```

Default is `StartsWith` because it is index-friendly. Override per property:

```csharp
[FilterField("Name")]
[StringSearchMode(StringSearchMode.Contains)]
public string? Name { get; init; }
```

**Custom predicates:**

When the attribute-driven system cannot express a condition, pass custom predicates:

```csharp
var filter = SearchQueryBuilder.BuildFilter<UserAgg, UserSearchQuery>(
    query,
    e => e.LoginName != "system",   // exclude system user
    e => e.CreatedAt > baseline     // additional business filter
);
```

Custom predicates are applied **before** attribute-driven filters, so they narrow the dataset first.

### BuildOrderBy

```csharp
public static Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? BuildOrderBy<TEntity>(SortQuery sort);

public static Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> BuildOrderByOrDefault<TEntity>(SortQuery sort);
```

| Method | Behavior |
|--------|----------|
| `BuildOrderBy` | Returns `null` if `sort.Fields` is empty |
| `BuildOrderByOrDefault` | Falls back to `OrderBy(e => e.Id)` if no sort fields provided |

**Multi-field sorting:** Fields are applied in order — first is primary, subsequent fields are tiebreakers.

### Caching

`SearchQueryMetaCache` stores `(EntityType, QueryType) → PropertyFilterMeta[]` in a static `ConcurrentDictionary`. This means:

- **First request** for a given pair reflects over the query type to discover decorated properties
- **Subsequent requests** reuse the cached metadata array
- The actual filter lambda is still built per request (it closes over the query instance), so there is no risk of stale closures

---

## PessimisticLockQueryBuilder

A fluent builder for constructing pessimistic lock SQL queries.

### Pessimistic Locking

Pessimistic locks are implemented via raw SQL using `PessimisticLockQueryBuilder`. Supported lock types:

| Lock Type Method   | PostgreSQL Clause | Use Case |
|--------------------|-------------------|----------|
| `ForUpdate()`      | `FOR UPDATE` | Exclusive lock for modification |
| `ForShare()`       | `FOR SHARE` | Shared lock for read |
| `ForNoKeyUpdate()` | `FOR NO KEY UPDATE` | Lock without blocking foreign key checks |
| `ForKeyShare()`    | `FOR KEY SHARE` | Minimal lock for key reads |

| Wait Strategy Method | PostgreSQL Clause | Use Case |
|----------------------|-------------------|----------|
| `NoWait()`           | `NOWAIT` | Fail immediately if lock unavailable |
| `SkipLocked()`       | `SKIP LOCKED` | Skip rows that are already locked |

These are chainable:

```csharp
public virtual async Task<TEntity?> GetByIdForUpdateAsync(Guid id,
    CancellationToken ct = default) =>
    await LockById(id).ForUpdate().BuildQuery().SingleOrDefaultAsync(ct);
```

### PessimisticLockQueryBuilder

`PessimisticLockQueryBuilder<TEntity>` is a single fluent builder that constructs raw-SQL lock queries and returns an `IQueryable<TEntity>`. Callers execute the query (e.g., `SingleOrDefaultAsync`, `ToListAsync`) after building.

```csharp
public sealed class PessimisticLockQueryBuilder<TEntity>
{
    public PessimisticLockQueryBuilder<TEntity> Where(
        string condition, params NpgsqlParameter[] parameters);

    public PessimisticLockQueryBuilder<TEntity> Where<TProperty>(
        Expression<Func<TEntity, TProperty>> propertyExpression, TProperty value);

    public PessimisticLockQueryBuilder<TEntity> WhereIn<TProperty>(
        Expression<Func<TEntity, TProperty>> propertyExpression,
        IReadOnlyCollection<TProperty> values);

    public PessimisticLockQueryBuilder<TEntity> ForUpdate();
    public PessimisticLockQueryBuilder<TEntity> ForShare();
    public PessimisticLockQueryBuilder<TEntity> ForNoKeyUpdate();
    public PessimisticLockQueryBuilder<TEntity> ForKeyShare();
    public PessimisticLockQueryBuilder<TEntity> NoWait();
    public PessimisticLockQueryBuilder<TEntity> SkipLocked();

    public IQueryable<TEntity> BuildQuery();
}
```

**Where methods:**
- `Where(string, params NpgsqlParameter[])` — raw SQL condition fragment
- `Where<TProperty>(expression, value)` — equality filter via expression tree; resolves column name from EF Core metadata and automatically applies value converters
- `WhereIn<TProperty>(expression, values)` — `IN (...)` filter via expression tree; empty collections produce `1 = 0`

**BuildQuery** returns an `IQueryable<TEntity>` backed by `FromSqlRaw`. Tracking is disabled automatically when `ForShare()` or `ForKeyShare()` is used; enabled when `ForUpdate()` or `ForNoKeyUpdate()` is used.

### Usage from EntityStoreBase

`EntityStoreBase` provides three protected helpers:

```csharp
protected PessimisticLockQueryBuilder<TEntity> Lock()
    => new (Context);

protected PessimisticLockQueryBuilder<TEntity> LockById(Guid id)
    => Lock().Where(x => x.Id, id);

protected PessimisticLockQueryBuilder<TEntity> LockByIds(IReadOnlyCollection<Guid> ids)
    => Lock().WhereIn(x => x.Id, ids);
```

- `Lock()` — starts a fresh builder for the entity type
- `LockById(id)` — pre-configured with `WHERE id = @p0`
- `LockByIds(ids)` — pre-configured with `WHERE id IN (@p0, @p1, ...)`

**Example — custom lock query:**

```csharp
// In UserRepository
public Task<UserAgg?> GetEnabledByLoginNameForUpdateAsync(string loginName, CancellationToken ct = default)
    => Lock()
        .Where(x => x.LoginName, loginName)
        .Where("status = @status", new NpgsqlParameter("status", MasterDataStates.Enabled.Value))
        .ForUpdate()
        .BuildQuery()
        .SingleOrDefaultAsync(ct);
```

The builder resolves table and column names from EF Core model metadata, so it works with configured schemas, column mappings, and value converters automatically.

---

## Design Principles

1. **Write repositories do not query.** `EntityStoreBase` provides only identifier-based loading. Complex filtering, sorting, and projection belong to the read side.

2. **Read repositories do not track.** `EntityQueryRepositoryBase` always uses `AsNoTracking()` and returns DTOs. Never return entities from the read side.

3. **Persistence is deferred.** Neither side calls `SaveChanges()`. The Unit of Work (`UnitOfWorkExecutor`) controls the transaction boundary.

4. **Pessimistic locking is explicit.** Lock methods are clearly named (`ForUpdate`, `ForShare`) and use raw SQL for database-native locking behavior.
