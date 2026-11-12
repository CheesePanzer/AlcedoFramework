# Alcedo.Core.Presentation

The Presentation layer provides the HTTP-facing infrastructure for Alcedo: controller bases, global exception handling, API versioning, model binders, JSON converters, and endpoint metadata discovery. MVC page controller features are excluded — they are not yet finalized.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Controller Base Classes](#controller-base-classes)
- [Endpoint Resource & Operation Center](#endpoint-resource--operation-center)
- [Global Exception Handling](#global-exception-handling)
- [ValueAtom Model Binder](#valueatom-model-binder)
- [JSON Converters](#json-converters)
- [API Versioning](#api-versioning)
- [Presentation Exceptions](#presentation-exceptions)
- [Utilities](#utilities)

---

## Architecture Overview

| Component | Purpose |
|-----------|---------|
| `AlcedoApiControllerBase` | Base API controller with route template and `CurrentUser` helper |
| `EndpointResourceOperationCenter` | Scans controllers for `[Resource]`/`[Operation]` attributes at startup |
| `AlcedoErrorExceptionHandler` | Global exception handler mapping `AlcedoException` → `ProblemDetails` |
| `ValueAtomModelBinder` | Binds query/route string values to `ValueAtom` subclasses |
| `JsonEmptyStringToNullConverter` | Deserializes `""` as `null` for string properties |
| `RequestIdempotencyKeyFilter` | Enforces idempotency rules (detailed in `02e`) |
| `IdempotencyKeyOperationFilter` | Adds `x-idempotency-key` header parameter to Swagger docs |

---

## Controller Base Classes

### AlcedoApiControllerBase

```csharp
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class AlcedoApiControllerBase : ControllerBase
{
    protected ContextUser CurrentUser => User.ToContextUser();
}
```

- Sets the standard route template: `api/v1/users`
- Provides `CurrentUser` — a typed wrapper over `ClaimsPrincipal`

### AlcedoApiAreaControllerBase

```csharp
[Area("Alcedo")]
[Route("api/v{version:apiVersion}/[area]/[controller]")]
public abstract class AlcedoApiAreaControllerBase : AlcedoApiControllerBase
```

For controllers within a named area: `api/v1/alcedo/roles`.

### ContextUser

```csharp
public sealed class ContextUser
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public bool IsAuthenticated { get; set; }
}
```

Extracted from `ClaimsPrincipal` via the `NameIdentifier` claim.

---

## Endpoint Resource & Operation Center

Controllers and actions declare resource/operation codes via attributes:

```csharp
[Resource("users", "User Management")]
public class UsersController : AlcedoApiControllerBase
{
    [Operation("list", "List Users")]
    public async Task<IActionResult> GetList(...) { }

    [Operation("create", "Create User")]
    public async Task<IActionResult> Create(...) { }
}
```

`EndpointResourceOperationCenter` scans all module assemblies at startup and builds a cache:

| Cache | Key | Value |
|-------|-----|-------|
| Permission keys | `(ControllerType, MethodInfo)` | `"area.resource.operation"` (lowercase) |
| Descriptions | `(ControllerType, MethodInfo)` | `(resourceDesc, actionDesc)` |

**Usage:**

```csharp
var key = _endpointCenter.GetKey(typeof(UsersController), method);           // "users.create"
var (resDesc, opDesc) = _endpointCenter.GetDescription(typeof(UsersController), method);
```

This metadata is used by the authorization system to map HTTP endpoints to permission entries.

---

## Global Exception Handling

`AlcedoErrorExceptionHandler` implements ASP.NET Core's `IExceptionHandler`:

```csharp
public class AlcedoErrorExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
}
```

**Status code mapping:**

| Exception Type | HTTP Status |
|----------------|-------------|
| `AlcedoNotFoundException` | 404 |
| `AlcedoUnauthorizedException` | 401 |
| `AlcedoForbiddenException` | 403 |
| `AlcedoBadRequestException` | 400 |
| `AlcedoConflictException` | 409 |
| `AlcedoTooManyRequestsException` | 429 |
| `AlcedoException` | 500 |
| `DBConcurrencyException` | 409 |
| Unhandled | 500 |

**ProblemDetails response:**

```json
{
  "status": 409,
  "title": "Conflict",
  "detail": "This record is referenced by other records",
  "type": "https://httpstatuses.com/409",
  "instance": "/api/v1/users/550e8400-e29b-41d4-a716-446655440000",
  "traceId": "00-abc123...",
  "innerErrorCode": "DomErrAggReferenced"
}
```

- `innerErrorCode` is the exception type name (for client-side error handling)
- `clientAction` is included if the exception provides a recovery hint

---

## ValueAtom Model Binder

ASP.NET Core's model binder cannot instantiate abstract types. `ValueAtomModelBinder` bridges query string / route values to concrete `ValueAtom` subclasses.

```csharp
public class ValueAtomModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        // Reads string value from query/route
        // Activator.CreateInstance(bindingContext.ModelType, value)
    }
}
```

**Example:**

```csharp
// GET /api/v1/users?status=Enabled
public async Task<IActionResult> GetList([FromQuery] MasterDataStateValue? status) { }
```

The binder receives `"Enabled"` and creates `new MasterDataStateValue("Enabled")`.

`ValueAtomModelBinderProvider` is registered at index 0 so it takes precedence over default binders.

---

## JSON Converters

Alcedo registers three custom JSON converters:

### JsonStringEnumConverter

Serializes enums as strings instead of integers:

```csharp
// "Enabled" instead of 0
JsonSerializer.Serialize(MasterDataStates.Enabled);
```

### JsonEmptyStringToNullConverter

Deserializes `""` as `null` for string properties. This prevents empty strings from polluting the database and simplifies validation.

```csharp
// Request body: { "email": "" }
// Deserializes to: Email = null
```

### ValueAtomConverterFactory

Serializes all `ValueAtom` subclasses as plain strings:

```csharp
// "Enabled" instead of { "value": "Enabled" }
JsonSerializer.Serialize(MasterDataStates.Enabled);
```

The factory handles all non-abstract `ValueAtom` subclasses automatically.

---

## API Versioning

Alcedo uses `Asp.Versioning` for API versioning:

```csharp
services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),        // /api/v1/users
        new HeaderApiVersionReader("x-api-version"),
        new QueryStringApiVersionReader("api-version")
    );
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

| Source | Example |
|--------|---------|
| URL segment | `/api/v1/users` |
| Header | `x-api-version: 2.0` |
| Query string | `?api-version=2.0` |

---

## Presentation Exceptions

Exceptions thrown at the HTTP boundary (model binding, idempotency, request validation):

| Exception | HTTP Status | Trigger |
|-----------|-------------|---------|
| `PreErrRequestInvalid` | 400 | Model state validation failure |
| `PreErrIdempotencyKeyRequired` | 400 | Missing `x-idempotency-key` header |
| `PreErrIdempotencyKeyExpired` | 400 | Strict idempotency key not found |
| `PreErrIdempotencyDuplicate` | 409 | Duplicate soft/content-based request |
| `PreErrIdempotencyFieldMissing` | 400 | Content-based idempotency field not found |

Invalid model state is converted to `PreErrRequestInvalid` instead of the default 400 response, so it flows through the unified exception handler and produces a consistent `ProblemDetails` format.

---

## Utilities

### HttpContextIpHelper

Extracts the client IP address from forwarded headers:

```csharp
public static string GetClientIp(HttpContext context)
```

Priority:
1. `X-Real-IP` header
2. First entry in `X-Forwarded-For` header
3. `RemoteIpAddress`
4. `"unknown"`

### IdempotencyKeyOperationFilter

Swagger operation filter that automatically adds the `x-idempotency-key` header parameter to all non-GET operations:

```csharp
public class IdempotencyKeyOperationFilter : IOperationFilter
```

This ensures the Swagger UI shows the idempotency header for write endpoints without manual annotation on every action.
