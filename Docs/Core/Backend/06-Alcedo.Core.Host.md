# Alcedo.Core.Host

The Host layer is the application bootstrapper. It wraps `WebApplicationBuilder` into a fluent `AlcedoBuilder`, orchestrates registration of all Core services, and provides opinionated defaults for Swagger, health checks, CORS, and the middleware pipeline.

You do not write business logic here. This layer answers the question: *"How do I wire everything together?"*

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [AlcedoBuilder](#alcedobuilder)
- [Service Registration](#service-registration)
  - [AddCore](#addcore)
  - [AddModules](#addmodules)
  - [AddAlcedoCoreHostEntry](#addalcedocorehostentry)
- [Middleware Pipeline](#middleware-pipeline)
  - [UseAlcedoCoreOnStartUp](#usealcedocoreonstartup)
  - [UseAlcedoCoreMiddlewares](#usealcedocoremiddlewares)
  - [MapAlcedoCoreControllers](#mapalcedocorecontrollers)
- [Swagger Integration](#swagger-integration)
- [Health Checks](#health-checks)
- [CORS Policy](#cors-policy)
- [Typical Program.cs](#typical-programcs)

---

## Architecture Overview

`Alcedo.Core.Host` sits above all other Core layers. It depends on `Alcedo.Core.Infrastructure`, `Alcedo.Core.Domain`, `Alcedo.Core.Application`, and `Alcedo.Core.Presentation`, calling their registration extension methods in the correct order.

| Component | Responsibility |
|-----------|---------------|
| `AlcedoBuilder` | Fluent wrapper around `WebApplicationBuilder` |
| `AddCore()` | Registers all Core services (Infra → Domain → Application → Presentation) |
| `AddModules()` | Iterates registered modules and calls their `AddServicesToBuilder` |
| `AddAlcedoCoreHostEntry()` | Swagger, health checks, ProblemDetails, exception handler, CORS |
| `UseAlcedoCoreOnStartUp()` | One-time initializations at pipeline start |
| `UseAlcedoCoreMiddlewares()` | Standard middleware pipeline |
| `MapAlcedoCoreControllers()` | Controller routes + health check endpoint |

---

## AlcedoBuilder

A thin wrapper that carries both the `WebApplicationBuilder` and the `AlcedoModulesRegistry` through the fluent chain:

```csharp
public sealed class AlcedoBuilder(WebApplicationBuilder webBuilder, AlcedoModulesRegistry registry)
{
    public WebApplicationBuilder WebBuilder { get; }
    public AlcedoModulesRegistry Registry { get; }
    public IConfiguration Configuration => WebBuilder.Configuration;
    public IServiceCollection Services => WebBuilder.Services;
    public ConfigureHostBuilder Host => WebBuilder.Host;
}
```

**Creation:**

```csharp
var builder = WebApplication
    .CreateBuilder(args)
    .CreateAlcedoBuilder(registry);
```

---

## Service Registration

### AddCore

One call registers all Core layers in dependency order:

```csharp
builder.AddCore();
```

This internally calls:

```csharp
services.AddAlcedoCoreInfrastructure(host, configuration, registry);  // EF Core, Redis, Quartz
services.AddAlcedoCoreDomain(registry);                                 // AggregateRoot metadata
services.AddAlcedoCoreApplication();                                    // DTOs, query contracts
services.AddAlcedoCorePresentation(registry);                           // Controllers, API versioning
services.AddAlcedoCoreHostEntry(configuration);                         // Swagger, health, CORS
```

### AddModules

Registers all business modules declared in the registry:

```csharp
builder.AddModules();
```

This iterates `registry.Modules` and calls each module's `AddServicesToBuilder`, which triggers auto-DI scanning and any manual registrations.

### AddAlcedoCoreHostEntry

Opinionated host-level services:

```csharp
services.AddAlcedoCoreHostEntry(configuration);
```

**What it adds:**

| Service | Purpose |
|---------|---------|
| `AddEndpointsApiExplorer` | API endpoint discovery for Swagger |
| `AddAlcedoCoreSwaggerGen` | Swagger + Bearer JWT + Idempotency header filter |
| `AddAlcedoCoreHealthChecks` | PostgreSQL + Redis health probes |
| `AddProblemDetails` | Standardized error response format |
| `AddExceptionHandler<AlcedoErrorExceptionHandler>` | Global exception → ProblemDetails mapping |
| `AddAlcedoCorsPolicy` | CORS with configurable origins |

---

## Middleware Pipeline

### UseAlcedoCoreOnStartUp

One-time initialization that must run **before** any request-processing middleware:

```csharp
app.UseAlcedoCoreOnStartUp();
```

This triggers:

1. `UseAlcedoAggregateRootGraphCenter()` — scans EF Core model and caches aggregate hierarchies
2. `UseReferenceIntegrityTool()` — initializes compiled reference check delegates
3. `UseEndpointResourceOperationCenter()` — scans all controllers for `[Resource]`/`[Operation]` metadata

All three are **initialization side-effects** — the middleware calls `GetRequiredService` to force singleton construction.

### UseAlcedoCoreMiddlewares

Standard pipeline for request processing:

```csharp
app.UseAlcedoCoreMiddlewares();
```

Order:

1. **Swagger UI** (development only)
2. **HTTPS Redirection**
3. **Exception Handler** — catches all unhandled exceptions
4. **CORS** — applies configured policy
5. **Serilog Request Logging** — request/response logging

### MapAlcedoCoreControllers

Maps controller routes and health check endpoint:

```csharp
app.MapAlcedoCoreControllers();
```

| Route | Purpose |
|-------|---------|
| `api/v{version}/[controller]` | API controllers |
| `/health` | Health check endpoint (returns JSON with DB + Redis status) |

---

## Swagger Integration

`AddAlcedoCoreSwaggerGen` configures Swashbuckle with:

- **Bearer JWT auth** — `Authorization: Bearer {token}` header
- **Idempotency header** — automatically adds `x-idempotency-key` to non-GET operations via `IdempotencyKeyOperationFilter`
- **API versioning** — one Swagger document per API version (`v1`, `v2`, ...)
- **Nullable reference types** — `SupportNonNullableReferenceTypes()`

In development, `UseAlcedoCoreSwagger()` mounts the Swagger UI at `/swagger`.

---

## Health Checks

`AddAlcedoCoreHealthChecks` registers two probes:

| Name | Target | Tags |
|------|--------|------|
| `postgresql` | `DataSource:Postgres` connection | `db`, `postgres` |
| `redis` | `DataSource:Redis` connection | `cache`, `redis` |

The `/health` endpoint returns a JSON response compatible with HealthChecks UI. MassTransit has its own health check endpoint and is not included here.

---

## CORS Policy

Configured via `AlcedoCorsOptions:AllowedOrigins`:

```json
{
  "AlcedoCorsOptions": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

The policy allows:
- Any header
- Any method
- Credentials (cookies / auth headers)

Apply with `UseAlcedoCorsPolicy()` in the middleware pipeline.

---

## Typical Program.cs

A minimal Alcedo host setup looks like this:

```csharp
Serilog.Debugging.SelfLog.Enable(Console.Error);

AlcedoModulesRegistry registry = new AlcedoModulesRegistry()
    .AddAlcedoAdmin()
    .AddAlcedoIdentity();

var builder = WebApplication.CreateBuilder(args);

builder.CreateAlcedoBuilder(registry)
    .AddCore()
    .AddModules()
    .Build();

var app = builder.Build();

app.UseAlcedoCoreOnStartUp();

app.UseAlcedoCoreMiddlewares()
    .UseAlcedoAdminMiddlewares()
    .UseAlcedoIdentityMiddlewares();

app.MapAlcedoCoreControllers();

app.SeedAlcedoIdentity();

app.Run();
```

**Key rule:** `AddCore()` before `AddModules()`, so module services can depend on Core abstractions. Module middleware (auth, access logging) is added after `Build()` but before controller mapping.
