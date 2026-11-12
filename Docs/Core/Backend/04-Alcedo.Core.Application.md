# Alcedo.Core.Application

The Application layer defines read-side contracts, search query DTOs, paginated result types, and reusable search service components. It depends on `Alcedo.Core.Abstractions` and `Alcedo.Core.Domain` but has no knowledge of EF Core, HTTP, or messaging infrastructure.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [DTO Base Classes](#dto-base-classes)
- [Search Query Base Classes](#search-query-base-classes)
- [Sort and Pagination](#sort-and-pagination)
- [Search Query Attributes](#search-query-attributes)
- [Read Repository Contract](#read-repository-contract)
- [Search Service Components](#search-service-components)
- [Application Exceptions](#application-exceptions)

---

## Architecture Overview

The Application layer is thin by design. It does not contain business logic — that lives in the Domain layer. Instead, it provides:

- **Contracts** for read-side repositories (`IEntityQueryRepository`)
- **Data transfer objects** for API responses (`EntityDtoBase`, `AggregateRootDtoBase`)
- **Search query objects** with attribute-driven filtering (`EntitySearchQueryBase`)
- **Reusable components** for common read operations (`EntitySearchServiceComponent`)

| Concept | Location | Purpose |
|---------|----------|---------|
| `EntityDtoBase` | `Dto/` | Base DTO with `Id` |
| `AggregateRootDtoBase` | `Dto/` | Adds `Version`, `CreatedAt`, `UpdatedAt` |
| `EntitySearchQueryBase` | `SearchQuery/` | Base search filter with `Ids` |
| `AggregateRootSearchQueryBase` | `SearchQuery/` | Adds audit date range filters |
| `SortQuery` / `PaginationQuery` | `SearchQuery/` | Sorting and paging parameters |
| `PagedResult<T>` | `PagedResult.cs` | Wrapper for paginated API responses |
| `IEntityQueryRepository<TDto, TQuery>` | `IQueryRepository/` | Read-side repository contract |
| `EntitySearchServiceComponent` | `Component/` | Reusable read service via composition |
| Exceptions | `AppException/` | Application-layer error types (`AppErr*`) |

---

## DTO Base Classes

### EntityDtoBase

```csharp
public abstract record EntityDtoBase
{
    public required Guid Id { get; set; }
}
```

All entity DTOs derive from this. The `required` modifier ensures `Id` is always populated by the mapping layer (Mapster projection).

### AggregateRootDtoBase

```csharp
public abstract record AggregateRootDtoBase : EntityDtoBase
{
    public required int Version { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
}
```

Adds optimistic concurrency and audit fields. Read repositories always project to DTOs, never return entities.

---

## Search Query Base Classes

### EntitySearchQueryBase

```csharp
public abstract record EntitySearchQueryBase
{
    [FilterField("Id")] public IReadOnlyList<Guid>? Ids { get; init; }
}
```

The base for all search filter objects. Properties are **opt-in** — only decorated properties participate in filter building. All nullable properties are skipped when `null`.

### AggregateRootSearchQueryBase

```csharp
public abstract record AggregateRootSearchQueryBase : EntitySearchQueryBase
{
    [DateRangeFilter("CreatedAt", DateRangeBound.Start)] public DateTime? CreatedAtStart { get; init; }
    [DateRangeFilter("CreatedAt", DateRangeBound.End)]   public DateTime? CreatedAtEnd   { get; init; }
    [DateRangeFilter("UpdatedAt", DateRangeBound.Start)] public DateTime? UpdatedAtStart { get; init; }
    [DateRangeFilter("UpdatedAt", DateRangeBound.End)]   public DateTime? UpdatedAtEnd   { get; init; }
}
```

Extends the base with standard audit date range filters. Most read queries in business modules inherit from this.

**Example custom query:**

```csharp
public record UserSearchQuery : AggregateRootSearchQueryBase
{
    [FilterField("LoginName")]
    [StringSearchMode(StringSearchMode.StartsWith)]
    public string? LoginName { get; init; }

    [FilterField("Status")]
    public MasterDataStateValue? Status { get; init; }

    [FilterField("Email")]
    public string? Email { get; init; }
}
```

---

## Sort and Pagination

### SortQuery

```csharp
public sealed record SortField
{
    public string Field { get; init; } = string.Empty;
    public bool Descending { get; init; }
}

public sealed record SortQuery
{
    public IReadOnlyList<SortField>? Fields { get; init; } = null;
    public bool HasSort => Fields?.Count > 0;
}
```

Multi-field sorting: fields are applied in order — the first is primary sort, subsequent fields are tiebreakers.

**Example:**

```csharp
var sort = new SortQuery
{
    Fields =
    [
        new SortField { Field = "CreatedAt", Descending = true },
        new SortField { Field = "Name", Descending = false }
    ]
};
```

### PaginationQuery

```csharp
public sealed record PaginationQuery
{
    public int PageNo { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
```

### PagedResult

```csharp
public class PagedResult<T>
{
    public int PageNo { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(1.0 * TotalCount / PageSize);
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
}
```

Used as the standard API response wrapper for list endpoints.

---

## Search Query Attributes

The Application layer declares attributes that the Infrastructure `SearchQueryBuilder` consumes to construct `IQueryable<T>` filter expressions.

### FilterFieldAttribute

Maps a query property to an entity property:

```csharp
[FilterField("LoginName")]
public string? LoginName { get; init; }
```

### DateRangeFilterAttribute

Marks a property as a date range boundary:

```csharp
[DateRangeFilter("CreatedAt", DateRangeBound.Start)]
public DateTime? CreatedAtStart { get; init; }
```

Pairs are automatically matched by the shared `EntityPropertyName`.

### StringSearchModeAttribute

Controls how string filters are translated to SQL `LIKE` patterns:

```csharp
public enum StringSearchMode
{
    Exact,      // = value
    StartsWith, // LIKE 'value%'  (default)
    EndsWith,   // LIKE '%value'
    Contains    // LIKE '%value%'
}
```

```csharp
[FilterField("Name")]
[StringSearchMode(StringSearchMode.Contains)]
public string? Name { get; init; }
```

Default is `StartsWith` because it is index-friendly. Use `Contains` explicitly only when full-text search is acceptable.

### SearchQueryBuilderIgnoreAttribute

Excludes a property from automatic filter building:

```csharp
[SearchQueryBuilderIgnore]
public string? SomeComputedField { get; init; }
```

Use this when a property is used by application logic but should not generate a SQL filter condition.

---

## Read Repository Contract

```csharp
public interface IEntityQueryRepository<TDto, in TQuery>
    where TDto : EntityDtoBase
    where TQuery : EntitySearchQueryBase
{
    Task<(IReadOnlyList<TDto> Items, int TotalCount)> GetPagedAsync(
        TQuery query, PaginationQuery pagiQuery, SortQuery sortQuery, CancellationToken ct = default);

    Task<IReadOnlyList<TDto>> GetListAsync(
        TQuery query, SortQuery sortQuery, CancellationToken ct = default);

    Task<TDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
```

**Key rules:**
- Always returns DTOs, never entities
- Always uses `AsNoTracking()`
- Filter construction is delegated to `SearchQueryBuilder`
- Sorting uses `SearchQueryBuilder.BuildOrderByOrDefault`

---

## Search Service Components

Application services that need read capabilities use **composition** rather than inheritance:

### EntitySearchServiceComponent

```csharp
public abstract class EntitySearchServiceComponent<TDto, TQuery>
    where TDto : EntityDtoBase
    where TQuery : EntitySearchQueryBase
{
    protected readonly IEntityQueryRepository<TDto, TQuery> QueryRepository;

    public async Task<PagedResult<TDto>> GetPagedDtosAsync(
        TQuery query, SortQuery sortQuery, PaginationQuery pagiQuery, CancellationToken ct = default);

    public async Task<TDto> GetDtoByIdAsync(Guid id, CancellationToken ct = default);
}
```

### AggregateRootSearchServiceComponent

```csharp
public abstract class AggregateRootSearchServiceComponent<TDto, TSearchQuery>
    : EntitySearchServiceComponent<TDto, TSearchQuery>
    where TDto : AggregateRootDtoBase
    where TSearchQuery : AggregateRootSearchQueryBase
```

A thin specialization for aggregate root DTOs. Adds no new methods — it exists purely for type-level clarity.

---

## Composition Over Inheritance in Application Services

Alcedo applies **composition over inheritance** at the application service level. An application service does not inherit a monolithic base class. Instead, it **composes** multiple focused components via constructor injection and delegates to them.

**Example — UserApplicationService:**

```csharp
internal sealed class UserApplicationService : IUserApplicationService
{
    private readonly UserSearchServiceComponent _searchService;
    private readonly UserEditServiceComponent _editService;
    private readonly UserStatusServiceComponent _statusService;
    private readonly ApplicationLockProvider _lockProvider;
    private readonly IUnitOfWorkExecutor _executor;
    // ... other dependencies

    public UserApplicationService(
        UserSearchServiceComponent searchService,
        UserEditServiceComponent editService,
        UserStatusServiceComponent statusService,
        ApplicationLockProvider lockProvider,
        IUnitOfWorkExecutor executor,
        /* ... */)
    {
        _searchService = searchService;
        _editService = editService;
        _statusService = statusService;
        _lockProvider = lockProvider;
        _executor = executor;
        // ...
    }

    // Delegates read operations to the search component
    public Task<PagedResult<UserDto>> GetPagedDtosAsync(...)
        => _searchService.GetPagedDtosAsync(...);

    // Delegates CRUD operations to the edit component
    public Task<Guid> CreateAsync(...)
        => _editService.CreateAsync(...);

    // Delegates status changes to the status component
    public Task EnableByIdAsync(...)
        => _statusService.EnableByIdAsync(...);

    // Custom business logic lives in the service itself
    public async Task ArchiveByIdAsync(Guid id, Guid operId, CancellationToken ct)
    {
        using var _ = await _lockProvider.CreateLock().AddX<UserAgg>(id).LockAsync(ct: ct);

        await _executor.ExecuteAsync(async () =>
        {
            var user = await _userRepository.GetByIdForUpdateAsync(id, ct)
                ?? throw new DomErrAggNotFound(id);

            await _aggRefChecker.ThrowIfReferencedAsync<UserAgg>(id, ct);

            user.Archive(operId, operSnap);
            await _userRepository.ArchiveHistoryUserAsync(...);
            _userRepository.Remove(user);
        }, ct);
    }
}
```

**Why composition:**

| Component | Responsibility | Can Override? |
|-----------|---------------|---------------|
| `SearchServiceComponent` | Paged queries, GetById | Rarely — behavior is standard |
| `EditServiceComponent` | Create, Update, Delete with optimistic locking | Yes — `CreateAggregateInternal`, `UpdateAggregateInternal` |
| `StatusServiceComponent` | Enable, Disable | Rarely — standard state transitions |

By decomposing the application service into focused components:

- Each component has a **single responsibility**
- Components are **testable in isolation**
- The application service acts as a **coordinator** — it delegates standard operations to components and keeps custom orchestration logic local
- Components from different modules can be **recombined** without inheritance coupling

**Framework-level components** (in `Alcedo.Framework.Shared`) provide reusable implementations for common master data patterns, but concrete modules still compose them explicitly rather than inheriting a one-size-fits-all application service base.

---

## Application Exceptions

Application-layer exceptions mirror the domain exception taxonomy but are prefixed with `AppErr`:

| Exception | HTTP Status | Scenario |
|-----------|-------------|----------|
| `AppErrGeneric` | 400 | Generic application error |
| `AppErrQueryResultNotFound<TDto>` | 404 | Query result not found by ID |
| `AppErrValidationFailed` | 400 | Composite validation failure |

**Rule of thumb:**
- `DomErr*` — thrown by aggregates and domain services (invariants, state machines)
- `AppErr*` — thrown by application services (parameter checks, coordination logic)

This distinction makes it clear whether a failure is due to broken business rules or incorrect usage of the application API.
