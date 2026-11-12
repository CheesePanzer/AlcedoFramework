# 01 — Alcedo.Frontend.Abstraction

Alcedo.Frontend.Abstraction is the zero-dependency contract layer. It defines the "language" shared across all frontend code: DTOs, search queries, commands, API client attributes, Auto UI attributes, validation attributes, backend interface contracts, and state machine primitives. This project has no references to other Alcedo projects or third-party UI libraries.

---

## 1. API Client Attributes

Declare HTTP API contracts as C# interfaces. These attributes are consumed by `AlcedoDispatchProxy` (Core layer) to generate HTTP requests at runtime.

### 1.1 Interface-Level: `[ApiClient]`

```csharp
[ApiClient(Area = "identity", Controller = "auth", Version = 1)]
public interface IAuthClient
{
    [HttpPost("login")]
    Task<AuthTokenPasswordPhaseResult> LoginAsync([ToBody] AuthCommand command, CancellationToken ct = default);

    [HttpGet("profile")]
    Task<UserProfileDto> GetProfileAsync(CancellationToken ct = default);
}
```

**Properties:**

| Property | Default | Description |
|---|---|---|
| `Controller` | (required) | Controller route segment |
| `Area` | `null` | Optional area prefix |
| `Version` | `1` | API version number |

**URL construction:** `/api/v{Version}` + `/` + `{Area}/` (if present) + `{Controller}` + `/` + method template.

The example above produces:
- `POST /api/v1/identity/auth/login`
- `GET /api/v1/identity/auth/profile`

### 1.2 Method-Level HTTP Attributes

| Attribute | Usage |
|---|---|
| `[HttpGet("template")]` | GET request; optional route template |
| `[HttpPost("template")]` | POST request; optional route template |
| `[HttpPut("template")]` | PUT request; optional route template |
| `[HttpPatch("template")]` | PATCH request; optional route template |
| `[HttpDelete("template")]` | DELETE request; optional route template |

### 1.3 Parameter-Level Attributes

| Attribute | Effect |
|---|---|
| `[ToRoute]` | Binds parameter as a route segment (e.g. `{id}`) |
| `[ToBody]` | Sends parameter as the JSON request body |
| `[ToQuery]` | Sends parameter as query string parameters |

**Convention (when no explicit attribute is present):**
- Simple types (primitives, `string`, `Guid`, `DateTime`, `decimal`, nullable of any) → treated as **route** parameters
- GET requests with complex types → treated as **query** parameters (flattened)
- Non-GET requests with complex types → treated as **body**

### 1.4 CancellationToken Convention

Parameters of type `CancellationToken` are automatically excluded from route/query/body resolution and passed as the request cancellation token.

---

## 2. DTO Hierarchy

DTOs are `record` types forming an inheritance chain. Each layer adds fields relevant to its abstraction level.

```
EntityDtoBase
 └─ AggregateRootDtoBase        (+ Version, CreatedAt, UpdatedAt)
     └─ ReferenceDataDtoBase     (+ Code)
         └─ MasterDataDtoBase    (+ Name, Status, Creator, Updater)
```

### 2.1 `EntityDtoBase`

```csharp
public abstract record EntityDtoBase
{
    [AutoFormIgnoreRender]
    [AutoTableIgnoreRender]
    public Guid Id { get; set; }
}
```

All DTOs have a `Guid Id`. Hidden from auto-rendered forms and tables by default.

### 2.2 `AggregateRootDtoBase`

```csharp
public abstract record AggregateRootDtoBase : EntityDtoBase
{
    [AutoFormIgnoreRender]
    [AutoTableIgnoreRender]
    public int Version { get; set; }

    [AutoFormIgnoreRender]
    public DateTimeOffset CreatedAt { get; set; }

    [AutoFormIgnoreRender]
    public DateTimeOffset UpdatedAt { get; set; }
}
```

Adds optimistic concurrency version and audit timestamps. `Version` is hidden from both forms and tables; `CreatedAt`/`UpdatedAt` show in tables but not forms.

### 2.3 `ReferenceDataDtoBase`

```csharp
public abstract record ReferenceDataDtoBase : AggregateRootDtoBase
{
    public string Code { get; set; } = null!;
}
```

Adds a human-readable unique code. Projects to `ReferenceDataDto` for reference pickers.

### 2.4 `MasterDataDtoBase`

```csharp
public abstract record MasterDataDtoBase : ReferenceDataDtoBase
{
    public string Name { get; set; } = null!;

    [AutoComponentValueAtomOptions(typeof(MasterDataStates))]
    public MasterDataStateValue Status { get; set; } = null!;

    [AutoFormIgnoreRender] [AutoTableIgnoreRender]
    public OperatorDto Creator { get; set; } = null!;

    [AutoFormIgnoreRender] [AutoTableIgnoreRender]
    public OperatorDto Updater { get; set; } = null!;
}
```

The typical entity DTO. `Status` uses `ValueAtom` with predefined options, rendered as a dropdown. `Creator`/`Updater` are operator snapshots, hidden from auto UI.

### 2.5 Supporting DTOs

**`PagedResult<T>`** — standard pagination envelope:
```csharp
public record PagedResult<T>
{
    public required int PageNo { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public int TotalPages { get; }          // computed
    public required IReadOnlyCollection<T> Items { get; init; }
}
```

**`OperatorDto`** — user snapshot in audit/log records:
```csharp
public record OperatorDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string SnapShot => $"[{Code}] {Name}";
}
```

**`MasterDataAuditLogDto`** — operation log entry:
```csharp
public sealed record MasterDataAuditLogDto : EntityDtoBase
{
    public Guid EntityId { get; set; }
    public string OperationName { get; set; } = null!;
    public string FromStatusName { get; set; } = null!;
    public string ToStatusName { get; set; } = null!;
    public DateTimeOffset OperatedAt { get; set; }
    public OperatorDto Operator { get; set; } = null!;
    public string? Note { get; set; }
}
```

### 2.6 How to Define a Custom DTO

```csharp
// For a Master Data entity:
public record ProductDto : MasterDataDtoBase
{
    public decimal Price { get; set; }
    public string Category { get; set; } = null!;
}

// For a simpler entity (no status machine):
public record TagDto : ReferenceDataDtoBase
{
    public string Description { get; set; } = null!;
}
```

Additional properties are automatically picked up by `AutoTable` and `AutoForm` (unless decorated with ignore attributes).

---

## 3. SearchQuery Hierarchy

Search query objects mirror the DTO hierarchy. Properties are nullable — a null value means "no filter."

```
EntitySearchQueryBase
 └─ AggregateRootSearchQueryBase  (+ CreatedAtStart/End, UpdatedAtStart/End)
     └─ ReferenceDataSearchQueryBase (+ Code)
         └─ MasterDataSearchQueryBase (+ Name, Status)
```

### 3.1 `EntitySearchQueryBase`

```csharp
public abstract record EntitySearchQueryBase;
```

Empty base marker. All search queries inherit from here.

### 3.2 `AggregateRootSearchQueryBase`

```csharp
public abstract record AggregateRootSearchQueryBase : EntitySearchQueryBase
{
    public DateTimeOffset? CreatedAtStart { get; set; }
    public DateTimeOffset? CreatedAtEnd { get; set; }
    public DateTimeOffset? UpdatedAtStart { get; set; }
    public DateTimeOffset? UpdatedAtEnd { get; set; }
}
```

Adds date-range filters for `CreatedAt` and `UpdatedAt`.

### 3.3 `ReferenceDataSearchQueryBase`

```csharp
public abstract record ReferenceDataSearchQueryBase : AggregateRootSearchQueryBase
{
    public string? Code { get; set; }
}
```

### 3.4 `MasterDataSearchQueryBase`

```csharp
public abstract record MasterDataSearchQueryBase : ReferenceDataSearchQueryBase
{
    public string? Name { get; set; }

    [AutoComponentValueAtomOptions(typeof(MasterDataStates))]
    public MasterDataStateValue? Status { get; set; }
}
```

`Status` uses `[AutoComponentValueAtomOptions]` so the `AutoSearch` component renders it as a dropdown of `MasterDataStates`.

### 3.5 `MasterDataLogSearchQuery`

```csharp
public sealed record MasterDataLogSearchQuery : ReferenceDataSearchQueryBase
{
    public Guid AggregateId { get; set; }
    public string? OperatorSnap { get; set; }
}
```

Used when querying audit logs for a specific entity.

### 3.6 `PaginationQuery` and `SortQuery`

```csharp
public sealed record PaginationQuery
{
    public int PageNo { get; set; } = 1;
    [Range(1, 1000)] public int PageSize { get; set; } = 50;
}

public sealed record SortField
{
    public string Field { get; set; } = string.Empty;
    public bool Descending { get; set; }
}

public sealed record SortQuery
{
    public IReadOnlyList<SortField>? Fields { get; set; } = null;
    public bool HasSort => Fields?.Count > 0;
}
```

### 3.7 Sort Presets

Sort presets define which fields the user can sort by. They follow the same inheritance chain as DTOs:

```csharp
AggregateRootSortPreset    → CreatedAt, UpdatedAt
ReferenceDataSortPreset    → + Code
MasterDataSortPreset       → + Name
```

To customize, inherit and override `ExtraFields`:

```csharp
public class ProductSortPreset : MasterDataSortPreset
{
    protected override IEnumerable<string> ExtraFields =>
    [
        nameof(ProductDto.Price),
        ..base.ExtraFields
    ];
}
```

Pass the preset to `AutoSearch.SortFields` or build inline.

### 3.8 How to Define a Custom Search Query

```csharp
public record ProductSearchQuery : MasterDataSearchQueryBase
{
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
}
```

---

## 4. Command Hierarchy

Commands are `record` types for create/update operations.

```csharp
public abstract record MasterDataCreateCommandBase
{
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
}

public abstract record MasterDataUpdateCommandBase
{
    [AutoFormIgnoreRender]
    public int Version { get; init; }

    public string Name { get; init; } = null!;
}
```

`Version` in update commands is hidden from the form (handled internally for optimistic concurrency).

**Example: custom commands**

```csharp
public record ProductCreateCommand : MasterDataCreateCommandBase
{
    public decimal Price { get; init; }
    public string Category { get; init; } = null!;
}

public record ProductUpdateCommand : MasterDataUpdateCommandBase
{
    public decimal Price { get; init; }
    public string Category { get; init; } = null!;
}
```

---

## 5. Backend Interface Contracts

Interface contracts that API clients implement. These use the HTTP method attributes from section 1.

### 5.1 `IPagedQueryController<TDto, TQuery>`

```csharp
public interface IPagedQueryController<TDto, in TQuery>
{
    [HttpGet]
    Task<PagedResult<TDto>> GetListAsync(TQuery query, SortQuery sortQuery, PaginationQuery paginationQuery);

    [HttpGet("{id}")]
    Task<TDto> GetAsync([ToRoute] Guid id);
}
```

The foundation for any list+detail page. By convention on GET, `TQuery` → query params, `SortQuery` → query params (flattened), `PaginationQuery` → query params (flattened).

### 5.2 `IReferenceController<TDto, TSearch>`

```csharp
public interface IReferenceController<TDto, in TSearch>
    : IPagedQueryController<TDto, TSearch>
    where TDto : ReferenceDataDtoBase
    where TSearch : ReferenceDataSearchQueryBase
{ }
```

Marker interface — indicates the entity is reference data.

### 5.3 `IMasterDataController<TDto, TSearch, TCreate, TUpdate>`

```csharp
public interface IMasterDataController<TDto, in TSearch, in TCreateCommand, in TUpdateCommand>
    : IMasterDataEditController<TCreateCommand, TUpdateCommand>,
      IMasterDataLogController, IMasterDataStateController,
      IReferenceController<TDto, TSearch>
    where TDto : MasterDataDtoBase
    where TSearch : MasterDataSearchQueryBase
    where TCreateCommand : MasterDataCreateCommandBase
    where TUpdateCommand : MasterDataUpdateCommandBase
```

The complete CRUD + state + audit log contract for master data entities.

**Composed interfaces:**

```csharp
// Create / Update
public interface IMasterDataEditController<in TCreate, in TUpdate>
{
    [HttpPost] Task<Guid> CreateAsync([ToBody] TCreate command);
    [HttpPut("{id}")] Task UpdateAsync([ToRoute] Guid id, [ToBody] TUpdate command);
}

// State transitions
public interface IMasterDataStateController
{
    [HttpDelete("{id}")] Task DeleteAsync([ToRoute] Guid id);
    [HttpPatch("{id}/status:enable")] Task EnableAsync([ToRoute] Guid id);
    [HttpPatch("{id}/status:disable")] Task DisableAsync([ToRoute] Guid id);
}

// Audit log
public interface IMasterDataLogController
{
    [HttpGet("logs")]
    Task<PagedResult<MasterDataAuditLogDto>> GetAuditLogsAsync(
        MasterDataLogSearchQuery query, SortQuery sortQuery, PaginationQuery paginationQuery);
}
```

### 5.4 How to Declare a Custom API Client

Inherit `IMasterDataController` and add domain-specific endpoints:

```csharp
[ApiClient(Area = "inventory", Controller = "products", Version = 1)]
public interface IProductClient : IMasterDataController<ProductDto, ProductSearchQuery,
    ProductCreateCommand, ProductUpdateCommand>
{
    [HttpPatch("{id}/status:archive")]
    Task ArchiveAsync([ToRoute] Guid id, CancellationToken ct = default);

    [HttpGet("{id}/stock")]
    Task<StockDto> GetStockAsync([ToRoute] Guid id, CancellationToken ct = default);
}
```

All inherited endpoints (`GetListAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `EnableAsync`, `DisableAsync`, `GetAuditLogsAsync`) are available automatically.

---

## 6. Auto Component Attributes

Attributes controlling the auto-generated UI component system (AutoForm, AutoTable, AutoSearch).

### 6.1 Form Attributes

| Attribute | Applies to | Effect |
|---|---|---|
| `[AutoFormIgnoreRender]` | Property | Hides property from AutoForm |
| `[AutoRenderLabel("text")]` | Property | Overrides display label (default: property name) |
| `[AutoFormRenderHelper("text")]` | Property | Shows helper text below the field |
| `[AutoFormRenderReadOnly]` | Property | Renders field as read-only |
| `[AutoFormRenderTextArea(lines, maxLines)]` | Property | Renders as multi-line text area instead of single-line input |
| `[AutoFormCellWidth(width)]` | Property | Column span in the form grid (1-12, mapped from MudGrid xs/sm/md) |

### 6.2 Table Attributes

| Attribute | Applies to | Effect |
|---|---|---|
| `[AutoTableIgnoreRender]` | Property | Hides column from AutoTable |
| `[AutoTableLink(propName, template)]` | Class | Renders property as a hyperlink. `template` supports `{PropertyName}` replacement (e.g. `"/products/{Id}"`) |

### 6.3 ValueAtom Options

| Attribute | Applies to | Effect |
|---|---|---|
| `[AutoComponentValueAtomOptions(typeof(OptionsClass))]` | Property | Specifies the static class whose `ValueAtom` fields provide dropdown options |

**Example:**

```csharp
public class ProductStates
{
    public static readonly ProductStateValue Active = new("Active");
    public static readonly ProductStateValue Discontinued = new("Discontinued");
    public static readonly ProductStateValue OutOfStock = new("OutOfStock");
}

// On DTO:
[AutoComponentValueAtomOptions(typeof(ProductStates))]
public ProductStateValue Status { get; set; } = null!;
```

### 6.4 Property Ordering

`AutoComponentOrderedProperties.Get(type)` walks the type hierarchy from `object` downward to the most derived type, collecting `DeclaredOnly` properties at each level. This means base class properties render before subclass properties — which aligns with the DTO hierarchy.

---

## 7. Validation Attributes

Attributes for the `AutoValidator` system. Used by `AutoFormDialog` and `AutoValidatorService` (Blazor layer).

| Attribute | Checks |
|---|---|
| `[AutoValidateIgnore]` | Skip this property entirely |
| `[AutoValidateRequired]` | Value must not be null |
| `[AutoValidateStringRequired]` | String must not be null or whitespace |
| `[AutoValidateEmail]` | String must match email regex |
| `[AutoValidatePhone]` | String must match `+<country><number>` |
| `[AutoValidateRange(min, max)]` | Numeric value must be in range |
| `[AutoValidateStringLength(min, max)]` | String length must be in range |
| `[AutoValidateCustom(checkerType, methodName)]` | Calls a static method `public static string? Method(object? value)` — returns null on success, error message on failure |

**Implicit validation (no attribute needed):**
- Non-nullable `string` without `[AutoValidateStringRequired]` → checked as non-empty
- Non-nullable `Guid` → checked as non-empty
- Non-nullable reference types with null value → rejected

**Usage:**

```csharp
public record CreateUserCommand : MasterDataCreateCommandBase
{
    [AutoValidateRequired("Login name is required")]
    public string LoginName { get; init; } = null!;

    [AutoValidateStringRequired]
    [AutoValidateStringLength(8, 64)]
    public string Password { get; init; } = null!;

    [AutoValidateEmail]
    public string? Email { get; init; }

    [AutoValidatePhone]
    public string? Phone { get; init; }
}
```

**Custom validator example:**

```csharp
public static class BusinessRules
{
    public static string? CheckUniqueCode(object? value)
        => value is string s && s.Contains("FORBIDDEN")
            ? "Code contains forbidden characters"
            : null;
}

// On property:
[AutoValidateCustom(typeof(BusinessRules), nameof(BusinessRules.CheckUniqueCode),
    "Custom error override (optional)")]
public string Code { get; init; } = null!;
```

---

## 8. StateMachine Primitives

Frontend representations of the backend state machine. These allow the UI to render status badges and determine which operations are available.

### 8.1 `ValueAtom` and JSON Serialization

```csharp
[JsonConverter(typeof(ValueAtomConverterFactory))]
public abstract record ValueAtom(string Value);
```

`ValueAtom` is the base for all strongly-typed string wrappers. The `ValueAtomConverterFactory` serializes/deserializes to/from the inner string transparently — the API sees `"Enabled"`, the frontend code sees `MasterDataStates.Enabled`.

Derived types:
```csharp
public abstract record StateValue(string Value) : ValueAtom(Value);
public abstract record StateTransitionTrigger(string Value) : ValueAtom(Value);
public abstract record StateAction(string Value) : ValueAtom(Value);
```

### 8.2 State Machine (Flow)

**Define states and triggers:**
```csharp
public sealed record MyStateValue(string Value) : StateValue(Value);

public class MyStates
{
    public static readonly MyStateValue Draft = new("Draft");
    public static readonly MyStateValue Active = new("Active");
    public static readonly MyStateValue Archived = new("Archived");
}

public sealed record MyTrigger(string Value) : StateTransitionTrigger(Value);

public class MyTriggers
{
    public static readonly MyTrigger Submit = new("Submit");
    public static readonly MyTrigger Archive = new("Archive");
}
```

**Define the flow:**
```csharp
public class MyFlow : StateFlowBase<MyStateValue, MyTrigger>
{
    protected override IReadOnlyList<StateTransitionRule<MyStateValue, MyTrigger>> Transitions =>
    [
        new(MyStates.Draft, MyTriggers.Submit, MyStates.Active),
        new(MyStates.Active, MyTriggers.Archive, MyStates.Archived),
    ];
}
```

**Query the flow:**
```csharp
var flow = new MyFlow();
flow.CheckOperationValidity(MyStates.Draft, MyTriggers.Submit);     // true
flow.GetNextState(MyStates.Draft, MyTriggers.Submit);                // MyStates.Active
flow.GetAllowedOperations(MyStates.Draft);                           // [Submit]
```

### 8.3 Operation Guard

Guards control which actions (Update, Delete, Archive) are available for a given status.

```csharp
public sealed record MyAction(string Value) : StateAction(Value);

public class MyActions
{
    public static readonly MyAction Update = new("Update");
    public static readonly MyAction Delete = new("Delete");
}

public class MyGuard : StateActionGuardBase<MyStateValue, MyAction>
{
    protected override IReadOnlyDictionary<string, IReadOnlySet<MyStateValue>> Rules =>
        new Dictionary<string, IReadOnlySet<MyStateValue>>
        {
            [MyActions.Update.Value] = new HashSet<MyStateValue>
                { MyStates.Draft, MyStates.Active },
            [MyActions.Delete.Value] = new HashSet<MyStateValue>
                { MyStates.Draft },
        };
}
```

### 8.4 Built-in MasterData State Machine

The framework provides a default state machine for master data entities:

**States:** `Transient → Draft → Enabled ↔ Disabled`

**Built-in flows:**
- `MasterDataDefaultStatusFlow` — Create / Enable / Disable transitions
- `MasterDataDefaultOperationGuard` — Update (Draft/Enabled/Disabled), Delete (Draft/Disabled)
- `MasterDataArchiveOperationGuard` — same + Archive (Disabled)

To extend, inherit and override `Additional` / `Base` properties.

---

## 9. Helper Classes

### 9.1 `EmailChecker` / `PhoneNumberChecker`

Static validators used by the AutoValidator and also callable directly:

```csharp
EmailChecker.IsValid("test@example.com");        // true
PhoneNumberChecker.IsValid("+8613800138000");     // true
```

### 9.2 `PasswordStrengthChecker`

```csharp
int score = PasswordStrengthChecker.GetStrength("P@ssw0rd");  // 4
bool strong = PasswordStrengthChecker.IsStrong("P@ssw0rd");   // true
```

Strength criteria (1 point each, minimum 3 to be "strong"):
- Length >= 8
- Contains uppercase letter
- Contains digit
- Contains special character

### 9.3 `AutoValidator`

The static validation engine. In typical usage, `AutoValidatorService` (Blazor layer) wraps this and ties into MudBlazor forms, but the static method is available for any context:

```csharp
var errors = AutoValidator.Validate(myCommand);
if (errors.Count > 0)
{
    // display errors
}
```

---

## 10. `ApiBusinessException`

```csharp
public class ApiBusinessException : Exception
{
    public ApiBusinessException(string message) : base(message) { }
}
```

Thrown by `ApiErrorHandler` (Core layer) when the backend returns a non-success status code. The `message` is extracted from the `detail` field of the RFC 7807 ProblemDetails response. Caught by `AlcedoRequestService` to display error dialogs.

---

## 11. Quick Reference: Defining a New Entity Module

To wire up a new master-data entity, create these four types in Abstraction:

```csharp
// 1. DTO
public record OrderDto : MasterDataDtoBase
{
    public decimal TotalAmount { get; set; }
}

// 2. Search Query
public record OrderSearchQuery : MasterDataSearchQueryBase
{
    public decimal? MinAmount { get; set; }
}

// 3. Create Command
public record OrderCreateCommand : MasterDataCreateCommandBase
{
    public decimal TotalAmount { get; init; }
}

// 4. Update Command
public record OrderUpdateCommand : MasterDataUpdateCommandBase
{
    public decimal TotalAmount { get; init; }
}

// 5. API Client Interface
[ApiClient(Area = "sales", Controller = "orders", Version = 1)]
public interface IOrderClient : IMasterDataController<OrderDto, OrderSearchQuery,
    OrderCreateCommand, OrderUpdateCommand>
{
}
```

Then in a Blazor page, `MasterDataListPage` with these four type parameters gives you a full CRUD page with search, table, pagination, and status toolbar — no additional code needed.
