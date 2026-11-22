# Alcedo.Framework.Shared

Shared provides reusable building blocks for **Reference Data** and **Master Data** on top of Alcedo Core. Reference data adds a unique business `Code`; master data adds a name, lifecycle, operator snapshots, and audit events. Applications compose these components rather than copying a CRUD stack for each aggregate.

---

## Table of Contents

- [Registration](#registration)
- [Define an Aggregate](#define-an-aggregate)
- [Lifecycle and Operator Snapshots](#lifecycle-and-operator-snapshots)
- [Configure Persistence](#configure-persistence)
- [Commands and Application Components](#commands-and-application-components)
- [Expose an API](#expose-an-api)
- [Audit and Reference Checks](#audit-and-reference-checks)
- [Troubleshooting and Further Reading](#troubleshooting-and-further-reading)

---

## Registration

Add Shared to the module registry before Core and application module registration:

```csharp
registry.AddAlcedoFrameworkBase(); // Alcedo.Framework.Shared
```

The module registers generic repositories, search components, status components, audit-log queries, and `IMasterDataStatusValidator`. Core supplies the EF context, unit of work, mapping, and message transport. See [Framework Host Registration](00-Alcedo.Framework-Overview.md#register-the-host) for the complete host.

Register your business module as well, so the framework scans its EF configurations, service attributes, controllers, and aggregate metadata. A project reference alone does not register those services.

---

## Define an Aggregate

Use `ReferenceDataAggregateRootBase` when only a stable `Code` is needed. Use `MasterDataAggregateRootBase` when records need explicit enable/disable state, audit, and operator information.

Aggregate types must end in `AggRoot` or `AggregateRoot`, or declare an explicit `AggregateRootDescriptionAttribute`; metadata initialization rejects other names. The following complete aggregate illustrates the master-data base:

```csharp
using Alcedo.Core.Domain.DomException;
using Alcedo.Framework.Shared.Domain.Entity;

public sealed class CountryAggRoot : MasterDataAggregateRootBase
{
    private CountryAggRoot() { }

    public CountryAggRoot(string code, string name, OperatorRecord creator)
        : base(code, name, creator) { }

    public void Rename(string name, OperatorRecord updater)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomErrRuleViolation("Country name is required");

        base.Update(name, updater);
    }
}
```

`Code`, `Id`, `Version`, `CreatedAt`, and `UpdatedAt` come from the inherited bases. Domain methods should validate their own business invariants before changing fields. HTTP validation is an additional boundary, not a substitute for those rules.

---

## Lifecycle and Operator Snapshots

The default state flow is:

```text
Transient --Create--> Draft --Enable--> Enabled --Disable--> Disabled
                                        ^                    |
                                        +------Enable--------+
```

Construction performs `Create` and leaves the aggregate in `Draft`. Public state methods accept **`OperatorRecord`**, not separate operator ID/snapshot strings:

```csharp
var oper = new OperatorRecord(Guid.NewGuid(), "ADMIN", "Administrator");
var country = new CountryAggRoot("NZ", "New Zealand", oper);
country.Enable(oper);
country.Disable(oper);
```

`Creator` and `Updater` are `OperatorRecord` objects containing `Id`, `Code`, and `Name`. The constructor clones the updater snapshot so the two owned EF values are separate instances. Application services receive `ContextOper` and can convert it with `OperatorRecord.From(oper)` or `oper.ToRecord()`.

`MasterDataDefaultOperationGuard` permits Update in Draft/Enabled/Disabled and Delete in Draft/Disabled. `MasterDataArchiveOperationGuard` instead permits Delete only in Draft and Archive in Disabled. Archive support is a domain convention: a concrete module must implement the history storage and removal workflow.

Override `StatusFlow` or `ActionGuard` to extend the lifecycle. Illegal transitions raise `DomErrStatusInvalid`; disallowed guarded actions raise `DomErrStatusActionInvalid`.

**Delete is not a built-in soft delete.** `MasterDataEditServiceComponent.DeleteByIdAsync` calls the domain guard and then `Repository.Remove`, physically removing the entity at commit. Disable preserves the record; an archive workflow must be implemented explicitly.

---

## Configure Persistence

Use the matching EF configuration base so Shared conventions are applied:

```csharp
using Alcedo.Framework.Shared.Infrastructure.Configuration;

public sealed class CountryConfiguration : MasterDataConfigurationBase<CountryAggRoot>
{
}
```

`ReferenceDataConfigurationBase<T>` configures the unique Code index. `MasterDataConfigurationBase<T>` adds state value conversion and owned Creator/Updater values; Core configures the aggregate version as a concurrency token.

For aggregate child collections, use the Core child-entity configuration helpers. For references to other aggregates, define the reference metadata and maintain database constraints as appropriate. See [EF Core Configuration](../../Core/Backend/02a-Core.Infrastructure-EFCore.md) and [Advanced Infrastructure](../../Core/Backend/02d-Core.Infrastructure-Advanced.md).

The default write repositories are `IReferenceDataRepositoryBase<T>` and `IMasterDataRepositoryBase<T>`. Master-data repositories distinguish:

- enabled-only and unrestricted reads;
- tracking and `NoTracking` reads;
- `ForUpdate`/`ForShare` reads that need an appropriate transaction boundary;
- strict list reads that reject missing IDs and partial list reads that return available records.

Use an application unit of work for write operations; adding an aggregate to a repository alone does not commit it.

---

## Commands and Application Components

Create commands inherit `Code` and `Name`; update commands inherit the expected `Version` and `Name`:

```csharp
using Alcedo.Framework.Shared.Application.Command;

public sealed record CountryCreateCommand : MasterDataCreateCommandBase;
public sealed record CountryUpdateCommand : MasterDataUpdateCommandBase;
```

Build DTOs from `ReferenceDataDtoBase` or `MasterDataDtoBase`, and queries from their matching search-query bases. For a master-data module, compose:

- `MasterDataSearchServiceComponent<TAgg,TDto,TSearch>` for list/detail reads;
- a concrete `MasterDataEditServiceComponent<TAgg,TCreate,TUpdate>` for creation, update, and deletion;
- `MasterDataStatusServiceComponent<TAgg>` for enable/disable;
- `MasterDataLogSearchService<TAgg>` for audit history.

The edit component owns a unit-of-work boundary. Override `CreateAggregateInternal` and `UpdateAggregateInternal` to call your aggregate's domain methods. Optional before/after hooks support extra checks and related work. Register the concrete component with `[RegisterSelf]` or explicit DI registration; Shared cannot instantiate an application's abstract edit specialization automatically.

The update path locks/loads the aggregate and checks `command.Version` against the current aggregate version before applying changes. Clients should fetch the DTO, submit that version, and reload after a successful write. Do not manufacture a new version locally.

The existing [JuiceEditServiceComponent](../../../Samples/Alcedo.Framework.Sample/modules/JuiceStore/Alcedo.JuiceStore.Backend/Application/Service/JuiceEditServiceComponent.cs) and [JuiceApplicationService](../../../Samples/Alcedo.Framework.Sample/modules/JuiceStore/Alcedo.JuiceStore.Backend/Application/Service/JuiceApplicationService.cs) show the complete composition and constructor signatures.

---

## Expose an API

Derive a concrete controller from `MasterDataControllerBase<TDto,TSearch,TCreate,TUpdate>` and provide its application service. For read-only reference data, use `ReferenceDataApiControllerBase<TDto,TQuery>`.

These inherit secured Framework API bases. Their RBAC policy is `AlcedoAuthorizePolicy.Rbac`; supply `[Area]`, `[ApiVersion]`, and resource/operation metadata for your module. Do not assume the Core controller base alone has the Framework authorization convention.

Under the concrete controller route, master-data endpoints include:

```text
GET    /                    paged list
GET    /{id}                detail
POST   /                    create
PUT    /{id}                update, including Version
PATCH  /{id}/status:enable   enable
PATCH  /{id}/status:disable  disable
DELETE /{id}                guarded physical delete
GET    /logs                audit history
```

The actual prefix is `/api/v1/{area}/{controller}` for the area base. Core's default POST filter requires a fresh `X-Idempotency-Key`; see [Presentation](../../Core/Backend/05-Alcedo.Core.Presentation.md) for strict/soft key overrides and model binding.

---

## Audit and Reference Checks

State changes and guarded actions emit `MasterDataAuditLogEvent`, carrying `AggregateId`, aggregate identity, operation, before/after state values, note, and `OperatorRecord`. The base directly creates this concrete event; there is no required `CreateAuditLogEventInternal` override.

The Shared consumer persists events as `MasterDataAuditLogEntity`. Its identifier is `AggregateId`, not an `EntityId` property. Audits are delivered through messaging, so a successful state-change response does not imply the audit query has already observed the consumer's write.

Deletion checks incoming references with `IAggRefChecker`. Outgoing references that must target enabled master data can be validated through `IMasterDataStatusValidator`. Domain reference metadata, repository access, and transaction/locking choices work together; a state check without a compatible transaction is not a cross-request consistency guarantee.

---

## Troubleshooting and Further Reading

- **Unregistered aggregate metadata**: register the owning business module before Core initialization. In unit tests, use the existing metadata test fixture helpers.
- **Update conflict**: reload the DTO and resubmit with its actual Version; do not silently overwrite another edit.
- **Delete rejected while enabled**: disable first when the aggregate's guard permits deletion from Disabled. Archive-enabled aggregates can require a separate archive operation instead.
- **No audit row immediately after a write**: inspect transport/consumer health and allow for asynchronous projection.
- **Missing application service/component**: register concrete implementations; the generic Shared registrations do not construct your module-specific service automatically.

See [Identity](11-Alcedo.Framework.Identity.md) for operator authentication and permission assignment, [Core Transactions and Events](../../Core/Backend/02c-Core.Infrastructure-Transaction-Events.md) for Outbox flow, and [JuiceStore](../../../Samples/Alcedo.Framework.Sample/modules/JuiceStore/Alcedo.JuiceStore.Backend) for a working business module.
