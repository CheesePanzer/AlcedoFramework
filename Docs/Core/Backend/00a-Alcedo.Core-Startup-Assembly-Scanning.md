# Startup Assembly Scanning

Alcedo adopts a **modular monolith** architecture where all business capabilities are encapsulated in independent `IAlcedoModule`s. During startup, the system scans the assemblies of these modules to perform the following tasks:

- Register DI services (`[RegisterAs]` / `[RegisterSelf]`)
- Bind configuration classes (`AppSettingsBase` subclasses)
- Build the AggregateRoot metadata index
- Build the EF Core entity relationship graph
- Collect cross-aggregate reference-integrity configurations
- Register MassTransit Consumers
- Register Quartz Jobs
- Register Redis KeySpaces
- Scan OpenAPI Transformers
- Scan Controller endpoint permissions (Resource/Operation)

---

## Scanning Locations

### Add Phase (Service Registration)

| Scanner | Layer | Target Layer | What It Scans | Trigger |
|---------|-------|-------------|---------------|---------|
| `AggregateRootMetadataCenter.Initialize` | Domain | Domain | Classes inheriting `AggregateRoot` in each module's assembly, reads `[AggregateRootDescription]` | `AddAlcedoCoreDomain` |
| `AutoServiceRegister.AutoRegisterInAssembly` | Abstractions | Executing Assembly | Classes decorated with `[RegisterAs]` or `[RegisterSelf]` | Each module's `AddServicesToBuilder` → `ScanAndRegisterDiServiceInLocalAssembly` |
| `AppSettingsRegister.BindingAppSettingsToOptions` | Infrastructure | Infrastructure | Classes inheriting `AppSettingsBase` | `AddAlcedoCoreInfrastructure` |
| `AlcedoModelCustomizer.Customize` | Infrastructure | Infrastructure | Classes implementing `IEntityTypeConfiguration<>` in each module's assembly | `AddAlcedoCoreDbContext` → when DbContext is first created |
| `ReferenceIntegrityRegistry.AddReferenceIntegrityTool` | Infrastructure | Domain | Classes implementing `IAggregateRootReferenceConfiguration<>` | `AddAlcedoCoreInfrastructure` (assemblies passed in) |
| `MassTransitExtensions.AddAlcedoMassTransit` | Infrastructure | Application | Classes implementing `IConsumer<>` | `AddAlcedoCoreInfrastructure` (assemblies passed in) |
| `RedisExtensions.AddAlcedoRedis` | Infrastructure | Infrastructure | Classes inheriting `RedisKeySpaceBase` | `AddAlcedoCoreInfrastructure` (assemblies passed in) |
| `QuartzExtensions.AddAutoQuartzJobs` | Infrastructure | Application | Classes decorated with `[QuartzJobAttribute]` | `AddAlcedoCoreInfrastructure` (assemblies passed in) |
| `ProgramExtension.AddAlcedoCorePresentation` | Presentation | Presentation | Classes inheriting `ControllerBase` in Presentation-layer modules, reads `[Resource]` / `[Operation]` | `AddAlcedoCorePresentation` (assemblies passed in) |
| `AddAlcedoCoreOpenApi` | Host | Presentation | Classes implementing `IOpenApiDocumentTransformer` / `IOpenApiOperationTransformer` | `AddAlcedoCoreHostEntry` (assemblies passed in) |

### Use Phase (After Application Startup)

| Scanner | Layer | Target Layer | What It Scans | Trigger |
|---------|-------|-------------|---------------|---------|
| `UseAlcedoAggregateRootGraphCenter` | Infrastructure | Domain | All EntityTypes from `AlcedoDbContext.Model`, traverses Navigations to build the Aggregate Graph | `UseAlcedoCoreOnStartUp` |
| `UseReferenceIntegrityTool` | Infrastructure | — | **No actual scanning**. Only triggers first-time instantiation of the `ReferenceIntegrityConfigCenter` singleton. | `UseAlcedoCoreOnStartUp` |
| `UseEndpointResourceOperationCenter` | Presentation | — | **No actual scanning**. Only triggers first-time instantiation of the `EndpointResourceOperationCenter` singleton. | `UseAlcedoCoreOnStartUp` |

> **Note**: Framework.Identity's `PermissionSeeder` also scans Controller/Action metadata at startup to generate `PermEntity` records. Its logic mirrors Core's `EndpointPermissionCenter`.

---

## Discussion on the Two Use-Phase Methods

`UseAlcedoCoreOnStartUp` invokes three methods:

```csharp
public static WebApplication UseAlcedoCoreOnStartUp(this WebApplication app)
{
    app.UseAlcedoAggregateRootGraphCenter();   // ①
    app.UseReferenceIntegrityTool();           // ②
    app.UseEndpointResourceOperationCenter();  // ③
    return app;
}
```

### ① UseAlcedoAggregateRootGraphCenter (Must Keep)

This method **performs real scanning work**: it creates a service scope, resolves `AlcedoDbContext`, and recursively builds the Aggregate Graph from `DbContext.Model`.

**Why must it run in the Use phase?**
- `AlcedoDbContext` is not yet instantiated during the Add phase (the model has not been built).
- This scan depends on EF Core's `IModel`, which is only available after the DbContext is first created.

### ② UseReferenceIntegrityTool (Can Be Removed)

```csharp
public static IApplicationBuilder UseReferenceIntegrityTool(this IApplicationBuilder app)
{
    _ = app.ApplicationServices.GetRequiredService<ReferenceIntegrityConfigCenter>();
    return app;
}
```

During the Add phase, the singleton is already registered:

```csharp
services.AddSingleton<ReferenceIntegrityConfigCenter>(_ =>
    new ReferenceIntegrityConfigCenter(assemblies));
```

`ReferenceIntegrityConfigCenter`'s constructor calls `ReferenceMetadataFactory.Create(assemblies)`, which performs all reflection scanning and expression compilation.

**Conclusion**: `UseReferenceIntegrityTool` only eagerly instantiates the singleton. If removed, the singleton will be created automatically on first injection of `IAggRefChecker` or `IAggRefScanner`. Functionally identical.

> The only difference after removal: if `ReferenceMetadataFactory.Create` throws (e.g., a misconfigured reference class), the exception is deferred from **startup** to **first request**.

### ③ UseEndpointResourceOperationCenter (Can Be Removed)

```csharp
public static IApplicationBuilder UseEndpointResourceOperationCenter(this IApplicationBuilder app)
{
    _ = app.ApplicationServices.GetRequiredService<EndpointResourceOperationCenter>();
    return app;
}
```

During the Add phase, the singleton is already registered:

```csharp
services.AddSingleton<EndpointResourceOperationCenter>(_ =>
    new EndpointResourceOperationCenter(registry.Modules.ContainLayer(ModuleLayer.Presentation).Assemblies));
```

`EndpointResourceOperationCenter`'s constructor has already completed the Controller/Action reflection scan and key caching.

**Conclusion**: Same as ②—only eager instantiation. If removed, `AlcedoRbacHandler` will trigger instantiation on its first request. Functionally identical.

---

## Recommendation

```csharp
public static WebApplication UseAlcedoCoreOnStartUp(this WebApplication app)
{
    // Real scan work that depends on DbContext Model
    app.UseAlcedoAggregateRootGraphCenter();

    // The two below are only eager singleton instantiation.
    // The Add phase has already registered them with explicit assembly parameters.
    // They can be removed, or kept as explicit "startup eager initialization" markers.
    // app.UseReferenceIntegrityTool();
    // app.UseEndpointResourceOperationCenter();

    return app;
}
```

Keep them if you want "fail on startup" behavior (any misconfiguration throws immediately at startup). Remove them if you prefer a leaner startup pipeline.
