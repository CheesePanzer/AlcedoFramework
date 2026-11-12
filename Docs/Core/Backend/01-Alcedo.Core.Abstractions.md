# Alcedo.Core.Abstractions

This is the innermost layer of Alcedo Core. It defines the contracts, base types, and primitives that all other layers depend on. It contains **no implementation logic** and has no external dependencies beyond the .NET runtime.

The abstractions layer establishes:
- The module system contract
- Automatic DI registration attributes
- The exception hierarchy
- The event root primitive
- The `ValueAtom` value-object base

---

## Table of Contents

- [Module System](#module-system)
  - [`IAlcedoModule`](#ialcedomodule)
  - [`AlcedoModuleBase`](#alcedomodulebase)
  - [`AlcedoModulesRegistry`](#alcedomodulesregistry)
- [DI Auto-Registration](#di-auto-registration)
  - [`[RegisterAs]`](#registeras)
  - [`[RegisterSelf]`](#registerself)
  - [How It Works](#how-it-works)
- [Exception Hierarchy](#exception-hierarchy)
  - [`AlcedoException`](#alcedoexception)
  - [HTTP Status Mapping](#http-status-mapping)
- [Event Primitive](#event-primitive)
  - [`EventRoot`](#eventroot)
- [Value Object Primitive](#value-object-primitive)
  - [`ValueAtom`](#valueatom)
- [Configuration Primitive](#configuration-primitive)
  - [`AppSettingsBase`](#appsettingsbase)

---

## Module System

Alcedo uses a **modular monolith** architecture. Each business domain (Identity, Admin, etc.) is packaged as a module that self-registers its services, configurations, and endpoints.

### `IAlcedoModule`

The core contract every module must implement:

```csharp
public interface IAlcedoModule
{
    string ModuleCode { get; }
    string ModuleName { get; }
    Assembly Assembly { get; }
    IReadOnlyCollection<ModuleLayer> ContainedLayers { get; }
    void AddServicesToBuilder(IServiceCollection services, IConfiguration configuration,
        AlcedoModulesRegistry registry);
}
```

| Member | Purpose |
|--------|---------|
| `ModuleCode` | Unique code for the module. |
| `ModuleName` | Display name. Used as the default database schema for entities in this module. |
| `Assembly` | The assembly containing the module's types. Core scans this for EF configurations, Mapster profiles, and consumers. |
| `ContainedLayers` | Declares which DDD layers (`Infrastructure`, `Domain`, `Application`, `Presentation`) exist in this module's assembly. Used by startup scanners to skip unnecessary assembly scanning. |
| `AddServicesToBuilder` | Called during host startup. The module registers its services here. |

A single `IAlcedoModule` represents an **assembly-level boundary**. Multiple modules can be combined to form a complete business domain.

### `AlcedoModuleBase`

A convenience base class that modules should inherit from:

```csharp
public abstract class AlcedoModuleBase : IAlcedoModule
{
    public virtual string ModuleCode { get; }
    public virtual string ModuleName { get; }
    public Assembly Assembly { get; }

    public virtual IReadOnlyCollection<ModuleLayer> ContainedLayers => new List<ModuleLayer>()
    {
        ModuleLayer.Infrastructure,
        ModuleLayer.Domain,
        ModuleLayer.Application,
        ModuleLayer.Presentation
    };

    protected AlcedoModuleBase(string? moduleCode = null, string? moduleName = null)
    {
        var type = GetType();
        var defaultName = type.Name.ToLowerInvariant().Replace("module", "");

        ModuleCode = moduleCode ?? defaultName;
        ModuleName = moduleName ?? defaultName;

        Assembly = type.Assembly;
    }

    public void AddServicesToBuilder(IServiceCollection services, IConfiguration configuration,
        AlcedoModulesRegistry registry)
    {
        ScanAndRegisterDiServiceInLocalAssembly(services);
        AddServicesManually(services, configuration, registry);
    }

    protected virtual void AddServicesManually(IServiceCollection services, IConfiguration configuration,
        AlcedoModulesRegistry registry) { }
}
```

When you inherit from `AlcedoModuleBase`:
- `ModuleCode` and `ModuleName` are auto-derived from the class name (e.g. `AlcedoIdentityModule` → `alcedoidentity`).
- `Assembly` points to the module's own assembly.
- `ContainedLayers` defaults to **all four layers**. Override it if your module is a thin assembly that only contains, for example, `Domain` and `Application`.
- **Auto-DI scanning** is performed automatically on the module's assembly.
- Override `AddServicesManually` for explicit service registration.

**Example:**

```csharp
public class AlcedoIdentityModule : AlcedoModuleBase
{
    protected override void AddServicesManually(IServiceCollection services, IConfiguration configuration,
        AlcedoModulesRegistry registry)
    {
        services.AddScoped<IOperatorModuleService, OperatorModuleService>();
        services.AddAlcedoIdentityAuthentication(configuration);
    }
}
```

### `AlcedoModulesRegistry`

The central registry that collects all modules during host startup:

```csharp
public sealed class AlcedoModulesRegistry
{
    public IReadOnlyList<IAlcedoModule> Modules { get; }
    public IReadOnlyList<Assembly> Assemblies { get; }

    public AlcedoModulesRegistry AddModule(IAlcedoModule module);
    public AlcedoModulesRegistry AddModules(params IAlcedoModule[] modules);
    public AlcedoModulesRegistry AddModules(IEnumerable<IAlcedoModule> modules);
}
```

Fluent API for registration:

```csharp
var registry = new AlcedoModulesRegistry()
    .AddAlcedoIdentity()   // extension method
    .AddAlcedoAdmin();     // extension method
```

**Duplicate protection:** `AddModule` throws if the same assembly is registered twice.

### Filtering by Layer

Startup scanners can skip modules that do not contain the target DDD layer using the `ContainLayer` extension:

```csharp
public static class AlcedoModuleListExtensions
{
    public IReadOnlyCollection<Assembly> Assemblies { get; }
    public IReadOnlyCollection<IAlcedoModule> ContainLayer(ModuleLayer layer);
}
```

**Example:**

```csharp
// Scan only modules that have a Presentation layer for controllers
var presentationModules = registry.Modules.ContainLayer(ModuleLayer.Presentation);

// Scan only modules that have an Application layer for consumers / Mapster profiles
var appModules = registry.Modules.ContainLayer(ModuleLayer.Application);
```

This avoids loading and reflecting over assemblies that cannot possibly contain the target types, improving startup performance.

---

## DI Auto-Registration

Every module assembly is scanned for DI services automatically. You mark implementation classes with attributes instead of writing `services.AddXxx()` manually.

### `[RegisterAs]`

Registers the class as a specific interface:

```csharp
[RegisterAs(typeof(IUserRepository))]
internal class UserRepository : MasterDataRepositoryBase<UserAgg>, IUserRepository
{
}
```

Multiple interfaces:

```csharp
[RegisterAs(typeof(IUserCacheStore))]
[RegisterAs(typeof(IUserCacheProvider))]
internal class UserCacheStore : IUserCacheStore, IUserCacheProvider
{
}
```

**Validation:**
- Cannot mix `[RegisterAs]` and `[RegisterSelf]` on the same class.
- Multiple `[RegisterAs]` must have the same lifetime.

### `[RegisterSelf]`

Registers the class as itself:

```csharp
[RegisterSelf]
internal class UserDomainService
{
}
```

Custom lifetime (default is `Scoped`):

```csharp
[RegisterSelf(ServiceLifetime.Singleton)]
internal class SomeSingletonService
{
}
```

### How It Works

`AlcedoModuleBase` calls `AutoServiceRegister.AutoRegisterInAssembly(services, Assembly)` during startup. This scans every non-abstract class in the assembly and applies the attributes.

**Registration precedence:**
1. If the class has `[RegisterAs]` attribute(s), register as those interfaces.
2. Else if the class has `[RegisterSelf]`, register as itself.
3. Else, skip.

---

## Exception Hierarchy

All framework and business exceptions derive from `AlcedoException`. This enables unified global exception handling that maps exceptions to HTTP status codes.

### `AlcedoException`

```csharp
public abstract class AlcedoException : Exception
{
    public string Code { get; }           // Exception type name, used as error code
    public string? ClientAction { get; }  // Hint for client recovery
}
```

### HTTP Status Mapping

| Exception Type | HTTP Status | Use Case |
|----------------|-------------|----------|
| `AlcedoBadRequestException` | 400 | Invalid input, business rule violation |
| `AlcedoUnauthorizedException` | 401 | Missing or invalid identity |
| `AlcedoForbiddenException` | 403 | Identity valid but access denied |
| `AlcedoNotFoundException` | 404 | Resource does not exist |
| `AlcedoConflictException` | 409 | Optimistic concurrency, version conflict |
| `AlcedoTooManyRequestsException` | 429 | Rate limit exceeded |
| `AlcedoInternalException` | 500 | Framework or unexpected error |

**Example:**

```csharp
// Domain layer
public class DomErrAggNotFound : AlcedoNotFoundException
{
    public DomErrAggNotFound(Guid id) : base($"Record with {id} was not found.") { }
}

// Application layer
public class AppErrInvalidQuery : AlcedoBadRequestException
{
    public AppErrInvalidQuery(string detail) : base($"Invalid query: {detail}") { }
}
```

The global exception handler (`AlcedoErrorExceptionHandler`) catches these and returns `ProblemDetails` with the corresponding status code.

---

## Event Primitive

### `EventRoot`

The base type for all events in the system (domain events, application events, integration events):

```csharp
public abstract record EventRoot
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

Every event automatically gets:
- A unique `EventId`
- A `Timestamp` in UTC

**Extension helpers:**

```csharp
var eventName = domainEvent.GetShortName();      // "UserCacheRefreshEvent"
var fullType = domainEvent.GetAssemblyType();    // Assembly-qualified name
```

Domain events (see `Alcedo.Core.Domain`) derive from `DomainEventBase`, which inherits `EventRoot`.

---

## Value Object Primitive

### `ValueAtom`

The base type for all value objects in the framework, especially state machines and domain value objects:

```csharp
[JsonConverter(typeof(ValueAtomConverterFactory))]
public abstract record ValueAtom(string Value)
{
}
```

Characteristics:
- **Immutability:** Record semantics, `init`-only `Value`.
- **JSON Serialization:** Custom `ValueAtomConverterFactory` handles all non-abstract subclasses. Serializes to plain string, deserializes back to the correct subclass.
- **EF Core Support:** Use `ValueAtomConverter<T>` to store as `string` in the database.

**Defining a concrete value object:**

```csharp
public sealed record MasterDataStateValue(string Value) : StateValue(Value)
{
}

public class MasterDataStates
{
    public static readonly MasterDataStateValue Draft = new("Draft");
    public static readonly MasterDataStateValue Enabled = new("Enabled");
    public static readonly MasterDataStateValue Disabled = new("Disabled");
}
```

**JSON usage:**

```csharp
// Serialized as: "Enabled"
JsonSerializer.Serialize(MasterDataStates.Enabled);

// Deserialized back to MasterDataStateValue
JsonSerializer.Deserialize<MasterDataStateValue>("\"Enabled\"");
```

---

## Configuration Primitive

### `AppSettingsBase`

Base class for strongly-typed configuration sections:

```csharp
public abstract class AppSettingsBase
{
}
```

Use with `[AppSettingsDemo]` to annotate properties for documentation generation:

```csharp
public class JwtOptions : AppSettingsBase
{
    [AppSettingsDemo("issuer", "The JWT issuer claim.")]
    public string Issuer { get; set; } = string.Empty;

    [AppSettingsDemo("audience", "The JWT audience claim.")]
    public string Audience { get; set; } = string.Empty;
}
```

Auto Register in `Alcedo.Core.Infrastructure`:

```csharp
Infrastructure layer will auto bind and register classes inherit AppSettingsBase into IOptions<T>
```

---

## Summary

`Alcedo.Core.Abstractions` defines the contracts and primitives that make the framework extensible:

- **Modules** self-register via `IAlcedoModule`.
- **Services** auto-register via `[RegisterAs]` and `[RegisterSelf]`.
- **Exceptions** map cleanly to HTTP status codes.
- **Events** carry `EventId` and `Timestamp` by default.
- **Value objects** (`ValueAtom`) serialize cleanly to/from JSON and databases.

Everything in this layer is framework-agnostic. It depends only on `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Configuration`.
