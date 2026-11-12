# 02 — Alcedo.Frontend.Core

Alcedo.Frontend.Core is the module system and HTTP communication skeleton. It provides the `IAlcedoModule` contract for modular self-registration, the `AlcedoModulesRegistry` that coordinates all modules, and a `DispatchProxy`-based dynamic HTTP client that turns attributed C# interfaces into working API calls at runtime. It depends on `Alcedo.Frontend.Abstraction`.

---

## 1. Module System

### 1.1 `IAlcedoModule` Contract

```csharp
public interface IAlcedoModule
{
    string ModuleCode { get; }
    string ModuleName { get; }
    Assembly Assembly { get; }
    void AddServicesToBuilder(WebAssemblyHostBuilder builder);
}
```

Every business module implements this interface. The framework uses the `Assembly` property for assembly scanning (nav contributors, bar items, API client interfaces). `AddServicesToBuilder` is the hook for module-specific DI registration.

### 1.2 `AlcedoModuleBase`

```csharp
public abstract class AlcedoModuleBase : IAlcedoModule
{
    public virtual string ModuleCode { get; }
    public virtual string ModuleName { get; }
    public Assembly Assembly { get; }

    protected AlcedoModuleBase(string? moduleCode = null, string? moduleName = null)
    {
        var type = GetType();
        var defaultName = type.Name.ToLowerInvariant().Replace("module", "");

        ModuleCode = moduleCode ?? defaultName;
        ModuleName = moduleName ?? defaultName;
        Assembly = type.Assembly;
    }

    public virtual void AddServicesToBuilder(WebAssemblyHostBuilder builder) { }
}
```

By default, `ModuleCode` and `ModuleName` are derived from the class name (e.g., `AlcedoIdentityModule` → `"alcedoidentity"`). Override in the constructor to set custom values.

### 1.3 `AlcedoModulesRegistry`

```csharp
public sealed class AlcedoModulesRegistry
{
    public IReadOnlyList<IAlcedoModule> Modules { get; }

    public AlcedoModulesRegistry AddModule(IAlcedoModule module);
    public AlcedoModulesRegistry AddModules(params IAlcedoModule[] modules);
    public AlcedoModulesRegistry AddModules(IEnumerable<IAlcedoModule> modules);
    public string? GetModuleCodeByAssembly(Assembly assembly);
}
```

A registry that holds all modules and enforces one-module-per-assembly (throws `InvalidOperationException` on duplicate). The static extension `AlcedoModuleListExtensions` provides `.Assemblies` as a convenience projection.

**Usage:**

```csharp
var registry = new AlcedoModulesRegistry()
    .AddModules(new AlcedoIdentityModule(), new AlcedoAdminModule(),
                new AlcedoNotificationModule(), new AlcedoFormulaEngineModule());
```

The registry is registered as a singleton and consumed by both Core (for API client discovery) and Blazor (for nav/bar discovery).

---

## 2. Startup Configuration

### 2.1 `AddAlcedoFrontEndCore`

```csharp
public static class ProgramExtensions
{
    public static WebAssemblyHostBuilder AddAlcedoFrontEndCore(
        this WebAssemblyHostBuilder builder,
        AlcedoModulesRegistry registry,
        Action<AlcedoFrontendOptions>? configure = null);
}
```

This is the single entry point. It:

1. Registers the `AlcedoModulesRegistry` as a singleton
2. Calls `AddAlcedoDispatchClient` to scan all module assemblies for `[ApiClient]` interfaces and wire them up
3. Calls `AddServicesToBuilder` on every module for module-specific DI

### 2.2 `AlcedoFrontendOptions`

```csharp
public sealed class AlcedoFrontendOptions
{
    public string BackendUrl { get; set; } = string.Empty;
    public List<Type>? AdditionalClientHandlers { get; set; }
}
```

| Property | Purpose |
|---|---|
| `BackendUrl` | Base URL of the backend API (all HTTP clients use this) |
| `AdditionalClientHandlers` | Extra `DelegatingHandler` types added to the pipeline after the three built-in handlers |

**Typical Program.cs:**

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);

var registry = new AlcedoModulesRegistry()
    .AddModules(new AlcedoIdentityModule(), /* ... */);

builder.AddAlcedoFrontEndCore(registry, opt =>
{
    opt.BackendUrl = builder.HostEnvironment.BaseAddress;
});

builder.AddAlcedoFrontEndBlazor(registry, opt =>
{
    opt.AppDisplayName = "My App";
});

var host = builder.Build();
host.UseAlcedoIdentityModule();
await host.RunAsync();
```

---

## 3. Dynamic HTTP Proxy

The core of the HTTP layer. `AlcedoDispatchProxy` uses `System.Reflection.DispatchProxy` to generate a runtime proxy for each `[ApiClient]`-decorated interface. Every method call is intercepted and translated into an HTTP request.

### 3.1 Architecture Overview

```
Interface method call
        │
        ▼
AlcedoDispatchProxy.Invoke()
        │
        ├── HttpMethodResolver    → GET / POST / PUT / PATCH / DELETE
        ├── ParameterResolver     → route params / query params / body
        ├── UrlBuilder            → /api/v{version}/{area}/{controller}/{template}
        └── HttpDispatcher        → http.GetFromJsonAsync / http.PostAsJsonAsync / ...
```

### 3.2 Proxy Creation and Registration

During startup, for each `[ApiClient]` interface found via assembly scanning:

```csharp
var httpClientBuilder = builder.Services.AddHttpClient(name, c =>
    c.BaseAddress = new Uri(apiBaseUrl));

httpClientBuilder
    .AddHttpMessageHandler<CredentialsHandler>()
    .AddHttpMessageHandler<IdempotencyHandler>()
    .AddHttpMessageHandler<ApiErrorHandler>();

// Add additional custom handlers...

builder.Services.AddScoped(interfaceType, sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var http = factory.CreateClient(name);
    return AlcedoDispatchProxy.Create<TInterface>(http);
});
```

Each API client gets its own named `HttpClient` (via `IHttpClientFactory`) with the full handler pipeline. The proxy is registered as scoped.

### 3.3 `HttpMethodResolver`

Maps the method's attribute to an HTTP method:

```csharp
internal static class HttpMethodResolver
{
    public static (HttpMethod method, string? template) Resolve(MethodInfo method) =>
        method.GetCustomAttribute<HttpGetAttribute>()    is { } get    ? (HttpMethod.Get,    get.Template)
        : method.GetCustomAttribute<HttpPostAttribute>()   is { } post   ? (HttpMethod.Post,   post.Template)
        : method.GetCustomAttribute<HttpPutAttribute>()    is { } put    ? (HttpMethod.Put,    put.Template)
        : method.GetCustomAttribute<HttpPatchAttribute>()  is { } patch  ? (HttpMethod.Patch,  patch.Template)
        : method.GetCustomAttribute<HttpDeleteAttribute>() is { } delete ? (HttpMethod.Delete, delete.Template)
        : throw new InvalidOperationException($"{method.Name} missing Http method attribute");
}
```

### 3.4 `ParameterResolver`

Classifies each method parameter by attribute (explicit) or convention (implicit):

| Condition | Resolution |
|---|---|
| `CancellationToken` type | Ignored (passed as request CT) |
| `[ToRoute]` attribute | Route parameter — replaces `{name}` in the URL template |
| `[ToBody]` attribute | JSON request body |
| `[ToQuery]` attribute | Query string parameter |
| **Convention:** simple type | Route parameter |
| **Convention:** complex type + GET | Query string (flattened) |
| **Convention:** complex type + non-GET | Request body |

### 3.5 `QueryParamHelper` — Complex Object Flattening

Query parameters from complex objects are flattened into `key[index].subprop` or `key.prop` format:

```csharp
// Given:
public record SearchQuery { public string Name { get; set; } public int? Age { get; set; } }

// Call: GetListAsync(new SearchQuery { Name = "John", Age = 30 }, ...)
// Produces query string: ?Name=John&Age=30

// For collections:
// List<string> ids = ["a", "b"]
// Produces: ?ids[0]=a&ids[1]=b

// For nested objects:
// Produces: ?filter.name=value&filter.age=30
```

Special handling:
- `ValueAtom` → emits `.Value` as the parameter value
- `IEnumerable` → emits indexed entries
- Simple types → emitted directly
- Complex types → flattened with dot-notation keys (root-level props skip the root name)

### 3.6 `UrlBuilder`

Constructs the full URL:

```
/api/v{Version} [/ {Area}] / {Controller} [/ {Template}]
```

- Reads `[ApiClient]` from the interface type
- Appends the method's template if present
- Replaces `{paramName}` placeholders with route parameter values
- Appends query string via `QueryHelpers.AddQueryString`

### 3.7 `ReturnTypeHelper`

Extracts `T` from `Task<T>` return types. Methods returning `Task` (no result) produce `null`, which the dispatcher handles as fire-and-forget.

### 3.8 `HttpDispatcher`

Dispatches to the correct `HttpClient` method based on HTTP verb and return type:

| Verb | Has return type | Method |
|---|---|---|
| GET | Yes | `GetFromJsonAsync<T>` (or `GetStringAsync` for `string`) |
| GET | No | `Task.CompletedTask` |
| POST | Yes | `PostAsJsonAsync` + `ReadFromJsonAsync<T>` |
| POST | No | `PostAsJsonAsync` |
| PUT | Yes | `PutAsJsonAsync` + `ReadFromJsonAsync<T>` |
| PUT | No | `PutAsJsonAsync` |
| PATCH | Yes | `PatchAsJsonAsync` + `ReadFromJsonAsync<T>` |
| PATCH | No | `PatchAsJsonAsync` |
| DELETE | Yes | `DeleteAsync` + `ReadFromJsonAsync<T>` |
| DELETE | No | `DeleteAsync` |

The dispatcher uses a shared `JsonSerializerOptions` configured with `JsonSerializerDefaults.Web`, `ValueAtomConverterFactory`, and `JsonStringEnumConverter`.

---

## 4. HTTP Message Handler Pipeline

Every API client's `HttpClient` goes through three built-in handlers in order:

```
Request
  → CredentialsHandler
    → IdempotencyHandler
      → ApiErrorHandler
        → Network
```

### 4.1 `CredentialsHandler`

```csharp
public class CredentialsHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        return base.SendAsync(request, ct);
    }
}
```

Sets `credentials: "include"` on every request so the browser sends cookies (JWT is stored in a secure, HTTP-only cookie on the backend side).

### 4.2 `IdempotencyHandler`

```csharp
public class IdempotencyHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Method == HttpMethod.Post)
        {
            request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        }
        return base.SendAsync(request, ct);
    }
}
```

Automatically adds a unique `X-Idempotency-Key` header to all POST requests. The backend's `IdempotencyFilter` uses this to prevent duplicate processing.

### 4.3 `ApiErrorHandler`

```csharp
public class ApiErrorHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var response = await base.SendAsync(request, ct);

        if (response.IsSuccessStatusCode) return response;

        // Extract "detail" from RFC 7807 ProblemDetails JSON
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var detail = body.GetProperty("detail").GetString()!;

        throw new ApiBusinessException(detail);
    }
}
```

On any non-success status code, extracts the `detail` field from the ProblemDetails response and throws `ApiBusinessException`. This is caught by `AlcedoRequestService` (Blazor layer) and shown to the user as a dialog.

If response body parsing fails (unexpected format), the handler catches and wraps the error in another `ApiBusinessException`.

---

## 5. Adding Custom Handlers

Custom `DelegatingHandler` types can be injected into the pipeline via `AlcedoFrontendOptions.AdditionalClientHandlers`:

```csharp
// 1. Define the handler
public class TimingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var response = await base.SendAsync(request, ct);
        sw.Stop();
        Console.WriteLine($"{request.RequestUri} took {sw.ElapsedMilliseconds}ms");
        return response;
    }
}

// 2. Register it
builder.AddAlcedoFrontEndCore(registry, opt =>
{
    opt.BackendUrl = builder.HostEnvironment.BaseAddress;
    opt.AdditionalClientHandlers = [typeof(TimingHandler)];
});
```

Custom handlers are added **after** the three built-in handlers in the pipeline:

```
Credentials → Idempotency → ApiError → Custom1 → Custom2 → Network
```

Custom handlers are registered as scoped services automatically and injected via `IHttpClientBuilder.AddHttpMessageHandler<T>()`.

---

## 6. Module Lifecycle

### 6.1 Service Registration Phase

```
WebAssemblyHostBuilder
    │
    ├── AddAlcedoFrontEndCore()
    │       ├── Register AlcedoModulesRegistry as singleton
    │       ├── Scan assemblies for [ApiClient] interfaces
    │       ├── Register each as named HttpClient + DispatchProxy
    │       └── Call module.AddServicesToBuilder() for each module
    │
    └── AddAlcedoFrontEndBlazor()    ← (Blazor layer, covered in doc 03)
            ├── Register MudBlazor services
            ├── Scan assemblies for INavContributor / IBarItem
            └── Register ThemeStateProvider, GlobalOverlayService, etc.

↓ Build
    
WebAssemblyHost
    │
    └── UseAlcedoIdentityModule()    ← module-specific host-level init
            └── Wire up OnUnauthenticated → redirect to login
```

### 6.2 Module Example: Identity

```csharp
public class AlcedoIdentityModule : AlcedoModuleBase
{
    public override void AddServicesToBuilder(WebAssemblyHostBuilder builder)
    {
        builder.Services.AddAuthorizationCore();
        builder.Services.AddSingleton<AuthStateProvider>();
        var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
                         ?? throw new InvalidOperationException("ApiBaseUrl is not configured");
        builder.Services.AddHttpClient("refresh",
            c => c.BaseAddress = new Uri(apiBaseUrl));
    }
}
```

Identity registers `AuthorizationCore`, its own `AuthStateProvider`, and a dedicated `HttpClient` named `"refresh"` for the JWT refresh endpoint. Other modules (`Admin`, `Notification`, `FormulaEngine`) have empty overrides because they need no extra services.

### 6.3 Minimal Custom Module

```csharp
public class MyBusinessModule : AlcedoModuleBase
{
    // Optional: override ModuleCode/ModuleName via constructor
    public MyBusinessModule() : base(moduleCode: "mybiz", moduleName: "My Business") { }

    public override void AddServicesToBuilder(WebAssemblyHostBuilder builder)
    {
        // Register module-specific services here
        builder.Services.AddScoped<IMyService, MyService>();
    }
}
```

---

## 7. `AlcedoModuleListExtensions`

```csharp
public static class AlcedoModuleListExtensions
{
    extension(IReadOnlyCollection<IAlcedoModule> modules)
    {
        public IReadOnlyCollection<Assembly> Assemblies
            => modules.Select(m => m.Assembly).Distinct().ToList();
    }
}
```

A C# 13 extension providing `.Assemblies` directly on any `IReadOnlyCollection<IAlcedoModule>`. Used internally by both Core and Blazor startup to scan for types.

---

## 8. Internals Summary

All proxy internals are `internal` — consumers never interact with them directly. The public surface is:

| Public API | Role |
|---|---|
| `IAlcedoModule` / `AlcedoModuleBase` | Implement to define a module |
| `AlcedoModulesRegistry` | Collect and register modules |
| `AlcedoFrontendOptions` | Configure backend URL and custom handlers |
| `AddAlcedoFrontEndCore()` | Bootstrap everything |
| `[ApiClient]` + HTTP attributes | Declare API contracts |
| `ApiBusinessException` | Caught by UI layer for error display |

Everything else (`HttpMethodResolver`, `ParameterResolver`, `UrlBuilder`, `HttpDispatcher`, `TypeHelper`, handlers) works behind the scenes.
