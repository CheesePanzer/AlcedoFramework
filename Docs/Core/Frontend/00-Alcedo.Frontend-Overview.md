# Alcedo.Frontend Overview

Alcedo.Frontend is the Blazor WebAssembly SPA layer of the Alcedo modular monolith. It mirrors the backend's modular philosophy — each Framework business module (Identity, Notification, Admin, FormulaEngine) has a corresponding Frontend module that consumes shared toolbox libraries.

---

## Technology Stack

| Technology | Purpose |
|---|---|
| .NET 10 | Runtime and language |
| Blazor WebAssembly | SPA hosting model |
| MudBlazor 9.x | Material Design component library |
| Microsoft.Extensions.* | DI, HttpClient, Configuration |

---

## Module Map

```
┌──────────────────────────────────────────────────────────────────────┐
│                        Business Modules                              │
│  (consume toolbox, mirror Framework Identity/Admin/Notification)    │
│                                                                     │
│  ┌──────────────┐  ┌──────────┐  ┌──────────────┐  ┌─────────────┐ │
│  │   Identity   │  │  Admin   │  │ Notification │  │ FormulaEng  │ │
│  └──────┬───────┘  └────┬─────┘  └──────┬───────┘  └──────┬──────┘ │
│         │               │               │                  │        │
│         └───────────────┴───────────────┴──────────────────┘        │
│                                 │                                    │
│              depends on ┌───────┴───────┐                            │
│                          │    Blazor     │  ← UI components,         │
│              depends on  │               │    Auto system, layouts   │
│                          └───────┬───────┘                            │
│              depends on  ┌───────┴───────┐                            │
│                          │     Core      │  ← module system,          │
│                          │               │    HTTP dispatch proxy    │
│                          └───────┬───────┘                            │
│              depends on  ┌───────┴───────┐                            │
│                          │  Abstraction  │  ← DTOs, contracts,       │
│                          │               │    attributes, commands   │
│                          └───────────────┘                            │
└──────────────────────────────────────────────────────────────────────┘
```

**Dependency rule:** Toolbox layers are consumed by business modules. Abstraction has zero internal dependencies. Core depends on Abstraction. Blazor depends on both Core and Abstraction. Business modules depend on all three.

---

## Layer Responsibilities

### Toolbox Layers (the toolkit)

| Layer | Role | Key Contents |
|---|---|---|
| **Abstraction** | Contract language shared across all frontend code | DTOs, search queries, commands, `[ApiClient]` attributes, validation attributes, Auto UI attributes, ValueAtom/StateMachine representations, backend interface contracts |
| **Core** | Module system and HTTP communication skeleton | `IAlcedoModule`, `AlcedoModulesRegistry`, `DispatchProxy`-based dynamic HTTP client, message handler pipeline (Credentials / Idempotency / ApiError) |
| **Blazor** | Reusable UI infrastructure | Layouts, navigation/bar registries, `AlcedoRequestService`, Auto components (AutoForm/AutoTable/AutoSearch), MasterData page bases, Pagination, GlobalOverlay |

### Business Modules

| Module | Backend Counterpart | What It Provides |
|---|---|---|
| **Identity** | `Alcedo.Framework.Identity` | Login/logout, TOTP 2FA, user/role/permission CRUD, profile, token refresh |
| **Admin** | Framework-level admin tools | Access log viewer, Quartz job manager |
| **Notification** | `Alcedo.Framework.Notification` | Message template CRUD, message config management |
| **FormulaEngine** | `Alcedo.Framework.FormulaEngine` | Formula listing, detail, and test execution |

---

## Key Design Principles

### 1. Convention over Configuration

- API clients are declared as C# interfaces with attributes — no manual `HttpClient` wiring
- Auto UI components render forms, tables, and search panels from DTO/Command property types
- Navigation and toolbar items are auto-discovered from assembly types implementing `INavContributor` / `IBarItem`
- Method parameters are routed as body/route/query by convention: simple types → route, GET complex types → query, non-GET complex types → body

### 2. Reflection at Startup, Not Runtime

- `DispatchProxy` uses `System.Reflection.DispatchProxy` for one-time proxy generation per interface
- Assembly scanning for API clients, nav contributors, and bar items happens once at `WebAssemblyHostBuilder` time
- Auto component property discovery reads the type hierarchy once via `AutoComponentOrderedProperties`

### 3. Modular Self-Registration

Every module implements `IAlcedoModule` and contributes:
- **Services** via `AddServicesToBuilder` (authorization, custom handlers)
- **Navigation** via `INavContributor` implementations in the module assembly
- **Toolbar items** via `IBarItem` implementations in the module assembly
- **API clients** via interfaces decorated with `[ApiClient]` in the module assembly

### 4. Request Execution as a First-Class Concern

`AlcedoRequestService` provides a unified execution pipeline: confirmation dialog → global loading overlay → semaphore-based concurrency guard → exception handling → success notification → post-execution callbacks. Every non-trivial button in the application flows through it.

---

## Typical Startup `Program.cs`

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);

var registry = new AlcedoModulesRegistry()
    .AddModules(new AlcedoIdentityModule(), new AlcedoAdminModule(),
                new AlcedoNotificationModule(), new AlcedoFormulaEngineModule());

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

## Document Map

| Document | Layer | Topics |
|---|---|---|
| [`01-Alcedo.Frontend.Abstraction`](01-Alcedo.Frontend.Abstraction.md) | Abstraction | DTOs, search queries, commands, API client attributes, Auto UI attributes, validation attributes, backend interface contracts, ValueAtom/StateMachine |
| [`02-Alcedo.Frontend.Core`](02-Alcedo.Frontend.Core.md) | Core | Module system, modules registry, dynamic HTTP proxy internals, message handler pipeline, startup extensions |
| [`03-Alcedo.Frontend.Blazor`](03-Alcedo.Frontend.Blazor.md) | Blazor | Navigation/bar system, layouts, themes, GlobalOverlay, AlcedoRequestService, Auto components, MasterData page bases, shared components |

---

## Relationship with Backend

```
Frontend                          Backend
───────────────────────────────────────────
Abstraction  ← shared concepts →  Core.Abstractions (ValueAtom, exceptions)

[ApiClient]  ← HTTP contract  →  Presentation controllers
attributes      matching routes

IUserClient   ← REST API  →  /api/v1/identity/users/*
IAuthClient   ← REST API  →  /api/v1/identity/auth/*

AutoForm      ← renders form →  Command DTOs (server-side validation)
AutoTable     ← renders table →  DTO projections from read repositories

Idempotency-  ← X-Idempotency-Key →  IdempotencyFilter
Handler           header               on POST endpoints
```

The frontend and backend are independently deployable — the WASM app is served as static files while API calls target a configured `BackendUrl`. They share DTO/convention contracts but no runtime dependencies.
