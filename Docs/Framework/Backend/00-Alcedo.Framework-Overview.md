# Alcedo.Framework Backend — Usage Overview

The Framework modules build on Alcedo Core with master-data conventions, identity, scheduler administration, notifications, and configurable formulas. This guide follows the `develop` implementation and links to the public entry points used by an application host.

---

## Table of Contents

- [Choose the Modules](#choose-the-modules)
- [Register the Host](#register-the-host)
- [Prepare Infrastructure](#prepare-infrastructure)
- [Add a Business Module](#add-a-business-module)
- [Read the Module Guides](#read-the-module-guides)
- [Verify the Integration](#verify-the-integration)

---

## Choose the Modules

- **Shared** supplies code-keyed aggregates, master-data state transitions, operator snapshots, audit events, and CRUD service/controller bases.
- **Identity** supplies JWT authentication, refresh cookies, session validation, users, roles, and endpoint permissions.
- **Admin** exposes Quartz scheduler operations and includes the storage cleanup job. Quartz itself is registered by Core Infrastructure.
- **Notification** discovers message contracts and dispatches configured email templates asynchronously.
- **FormulaEngine** discovers typed formula contracts and executes expressions stored in the database.

Register Shared before the Framework modules that consume its generic services. The supplied Notification module also resolves `IUserQueryIdentityService` from Identity for its configuration test-send operation. Register Identity when using that workflow, or supply that contract yourself.

Referencing an assembly is not sufficient to discover its entities, services, message types, or formulas. Add its module to `AlcedoModulesRegistry` before calling `AddCore` and `AddModules`.

---

## Register the Host

The following is an ASP.NET Core `Program.cs` skeleton. Supply the infrastructure configuration and a migration assembly before running it.

```csharp
using Alcedo.Core.Abstractions;
using Alcedo.Core.Host;
using Alcedo.Framework.Admin;
using Alcedo.Framework.FormulaEngine;
using Alcedo.Framework.Identity;
using Alcedo.Framework.Notification;
using Alcedo.Framework.Shared;

var registry = new AlcedoModulesRegistry()
    .AddAlcedoFrameworkBase()
    .AddAlcedoIdentity()
    .AddAlcedoAdmin()
    .AddAlcedoNotification()
    .AddModule(new AlcedoFormulaEngineModule());

var builder = WebApplication.CreateBuilder(args);
builder.CreateAlcedoBuilder(registry)
    .AddCore(options =>
    {
        options.Infrastructure.MigrationAssembly = typeof(Program).Assembly;
        options.Infrastructure.UsePostgresMqTransfer = true;
        options.Presentation.DisplaySensitiveExceptions = false;
    })
    .AddModules()
    .Build();

var app = builder.Build();
app.UseAlcedoCoreOnStartUp();
app.UseAlcedoCoreMiddlewares()
    .UseAlcedoIdentityMiddlewares();
app.MapAlcedoCoreControllers();

// Run these only after the application's database schema has been applied.
app.SeedAlcedoIdentity();
app.SeedFormulaEngine();
app.Run();
```

`UseAlcedoCoreMiddlewares` establishes the Core pipeline and routing. Identity then adds authentication, `AuthSessionMiddleware`, and authorization, in that order. `AlcedoFmApiControllerBase` and `AlcedoFmApiAreaControllerBase` already require the Framework RBAC policy; a global `AuthorizeFilter` is not required for those bases.

There is no `AddAlcedoFormulaEngine` registry helper in the current implementation: use `AddModule(new AlcedoFormulaEngineModule())`. There is also no Admin-specific middleware call on the current `develop` branch.

See the [complete sample host](../../../Samples/Alcedo.Framework.Sample/back/Program.cs) for the same pipeline with the JuiceStore business module and MVC support.

---

## Prepare Infrastructure

Framework registration does not provision a complete environment by itself:

1. Configure PostgreSQL and Redis through `DataSourceOptions`.
2. Choose PostgreSQL message transport as above, or configure RabbitMQ for the default transport path.
3. Apply application EF Core migrations, including the Framework, Outbox, and Quartz model objects.
4. Supply `JwtOptions`, `SecurityOptions`, and `AlcedoColumnEncryptorOptions` for Identity.
5. Supply `SmtpOptions` when sending email and Core storage configuration when running storage cleanup.
6. Match frontend origin, API address, CORS settings, and HTTPS/cookie behavior.

Seeder methods create application data; they do not replace schema migration. The Identity seeder creates a development root account with fixed credentials. Change its password before exposing the application and see the [Identity guide](11-Alcedo.Framework.Identity.md#bootstrap-and-first-login) for the current first-login behavior.

For concrete environment variables, migration steps, and deployment files, use [Sample Deployment](../../Samples/Alcedo.Framework.Sample/Deployment.md). For the Core registration and configuration pipeline, use [Core Host](../../Core/Backend/06-Alcedo.Core.Host.md) and [Infrastructure Overview](../../Core/Backend/02-Alcedo.Core.Infrastructure-Overview.md).

---

## Add a Business Module

Keep domain rules in aggregates and domain services. Compose Framework application components for CRUD and state changes, and use a Framework controller base for secured API endpoints.

The [JuiceStore backend](../../../Samples/Alcedo.Framework.Sample/modules/JuiceStore/Alcedo.JuiceStore.Backend) demonstrates the full path:

```text
Module registration
    -> Aggregate + EF configuration
    -> Commands + DTO + search query
    -> Edit/search/status application components
    -> Application service
    -> Framework API controller
```

Use a Contracts project for the public cross-module message or service contract. A caller that only publishes a notification should depend on `Alcedo.Framework.Notification.Contracts`; the host still needs the implementation module and its consumers to deliver that notification.

---

## Read the Module Guides

- [10 — Shared](10-Alcedo.Framework.Shared.md): implement a master-data aggregate and expose CRUD/state operations.
- [11 — Identity](11-Alcedo.Framework.Identity.md): configure authentication, log in, refresh tokens, and assign permissions.
- [12 — Admin](12-Alcedo.Framework.Admin.md): register jobs and operate Quartz through the API.
- [13 — Notification](13-Alcedo.Framework.Notification.md): declare a payload, configure templates, dispatch, and retry errors.
- [14 — FormulaEngine](14-Alcedo.Framework.FormulaEngine.md): declare typed formulas, configure expressions, execute, and manage versions.

---

## Verify the Integration

Start with `/health/live` and `/health/ready`, then exercise authentication and the business operation you are integrating. A healthy database or successful unit test does not prove SMTP delivery, message transport, or browser cookie behavior.

The repository's [Testing Strategy](../../Testing/00-Testing-Strategy.md) distinguishes unit, architecture, API, and UI tests. The [test execution record](../../../Tests/TESTING.md) describes the added module tests and their integration limits. Keep provider-specific and end-to-end verification separate from isolated unit tests.
