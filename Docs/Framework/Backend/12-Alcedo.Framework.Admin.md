# Alcedo.Framework.Admin

Admin exposes **Quartz scheduler administration** through secured Framework endpoints. It also registers the scheduled storage cleanup job. Quartz registration, persistence, and the hosted scheduler are supplied by Core Infrastructure; the Admin module is the application-facing control surface.

---

## Table of Contents

- [Registration and Prerequisites](#registration-and-prerequisites)
- [Register an Application Job](#register-an-application-job)
- [Inspect and Operate the Scheduler](#inspect-and-operate-the-scheduler)
- [Change a Trigger](#change-a-trigger)
- [Storage Cleanup](#storage-cleanup)
- [Troubleshooting and Further Reading](#troubleshooting-and-further-reading)

---

## Registration and Prerequisites

Add `.AddAlcedoAdmin()` to the registry used by the [Framework host](00-Alcedo.Framework-Overview.md#register-the-host). Use Identity or an equivalent configuration of the Framework RBAC policy to authenticate and authorize the API.

Core scans registered module assemblies for `[QuartzJob]` classes and calls `AddAutoQuartzJobs`. The registry configures PostgreSQL persistence with table prefix `quartz.QRTZ_`, JSON serialization, and a hosted scheduler. Set `DataSourceOptions.Postgres` and apply the application's migrations before starting the scheduler.

There is no separate `UseAlcedoAdminMiddlewares()` call on current `develop`. The former AccessLog queue/controller implementation has also been removed; use the host's Serilog request logging rather than documenting the old AccessLog API as an active Admin feature.

---

## Register an Application Job

Place the job in a registered business module. The following job runs every five minutes using a Quartz cron expression with a seconds field:

```csharp
using Alcedo.Core.Infrastructure.Job;
using Microsoft.Extensions.Logging;

[QuartzJob(cronExpression: "0 0/5 * * * ?", jobName: "RefreshCatalog", jobGroup: "Catalog")]
public sealed class RefreshCatalogJob : AlcedoJobBase
{
    public RefreshCatalogJob(IServiceProvider sp, ILoggerFactory loggerFactory)
        : base(sp, loggerFactory) { }

    protected override Task ExecuteAsync(
        string jobName,
        IDictionary<string, object> data,
        IServiceProvider sp,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Logger.LogInformation("Refreshing the catalog from {JobName}", jobName);
        return Task.CompletedTask;
    }
}
```

Resolve application dependencies from the `sp` supplied to `ExecuteAsync`: the base creates that execution scope and provides logging/trace context. Propagate `ct` to the actual work. An exception is logged and rethrown by the base; a job implementation should not silently convert failed work into a successful run.

The automatic job key above is group `Catalog`, name `RefreshCatalog`; its default trigger is named `RefreshCatalog-trigger` in the same group. A positive `intervalInSeconds` is an alternative to cron in `QuartzJobAttribute`. Cron takes precedence when supplied.

The registry uses `IgnoreDuplicates=true` and `OverWriteExistingData=false`. A persisted trigger changed through the API is therefore not necessarily replaced merely by editing the attribute and restarting the host.

---

## Inspect and Operate the Scheduler

All endpoints below are relative to `/api/v1/admin/quartz` and require Bearer authentication plus the appropriate endpoint permission. For example, the list permission is `admin.quartz.list`.

```text
GET  /jobs                              list registered jobs and their triggers
GET  /scheduler/status                  inspect scheduler lifecycle state
POST /jobs/{group}/{name}/trigger        request an immediate run
POST /jobs/{group}/{name}/pause          pause a job
POST /jobs/{group}/{name}/resume         resume a job
POST /scheduler/pause                    pause all scheduling
POST /scheduler/resume                   resume all scheduling
```

`GET /jobs` returns `JobDto` objects containing Name, Group, Description, and Triggers. Each `TriggerDto` includes Name, Group, NextFireTime, PreviousFireTime, and State. Use the returned identifiers rather than assuming every trigger has the default generated name.

Example manual invocation:

```http
POST /api/v1/admin/quartz/jobs/Catalog/RefreshCatalog/trigger
Authorization: Bearer ACCESS_TOKEN
X-Idempotency-Key: a-new-unique-request-key
```

A successful trigger request means Quartz accepted the scheduling request, not that the job's work has already finished. Inspect job logs or application results to establish execution success.

---

## Change a Trigger

Use the job and trigger identifiers returned by `/jobs`:

```text
POST   /jobs/{group}/{name}/triggers/{triggerGroup}/{triggerName}/pause
POST   /jobs/{group}/{name}/triggers/{triggerGroup}/{triggerName}/resume
PUT    /jobs/{group}/{name}/triggers/{triggerGroup}/{triggerName}/cron
DELETE /jobs/{group}/{name}/triggers/{triggerGroup}/{triggerName}
DELETE /jobs/{group}/{name}
```

The Cron update body is `UpdateCronRequest`:

```json
{ "cronExpression": "0 0/10 * * * ?" }
```

`IQuartzAppService.AddTriggerAsync(group, name, AddTriggerRequest)` also supports adding a trigger from application code. The current controller does **not** expose an AddTrigger HTTP action, so do not infer an API route from the service method alone.

The implementation checks job and trigger existence separately. Callers should select a trigger that actually belongs to the requested job; the current service does not enforce that association in all operations. Updating Cron builds a replacement trigger, so review schedule-specific settings rather than assuming custom timezone or misfire settings are retained.

Pause is reversible scheduler state. Delete removes the job or trigger configuration; use it only when that removal is intended. These operations do not constitute a general-purpose transaction with the business work performed by the job.

---

## Storage Cleanup

The module's `StorageCleaner` job is registered as:

```text
Group:   Admin
Job:     StorageCleaner
Trigger: StorageCleaner-trigger
Cron:    0 0 0 * * ?
```

The cron represents a daily midnight schedule in the scheduler's applicable timezone. The job invokes `IStorageGcExecutor`, which consumes pending `StorageGcTask` records and deletes the corresponding objects through Core file storage.

The complete path is:

```text
EF save changes with storage-key changes/removals
    -> StorageGcInterceptor records GC tasks
    -> StorageCleaner invokes IStorageGcExecutor
    -> storage provider deletion
```

Configure the Core storage provider and bucket/key metadata as well as PostgreSQL. Registering Admin does not create a storage backend or immediately delete every unreferenced object in a bucket. Failed deletions are logged and retain retry/error data in the task table.

---

## Troubleshooting and Further Reading

- **No application job in `/jobs`**: check `[QuartzJob]`, the class being non-abstract, and registration of the owning module assembly.
- **Quartz database error on startup**: confirm connection settings, migrations, and the `quartz` schema/tables.
- **Attribute schedule appears ignored after restart**: inspect persisted triggers and the no-overwrite scheduling policy before expecting an attribute change to replace them.
- **Immediate trigger returned success but work failed**: inspect the job execution log, not only the trigger endpoint response.
- **Storage tasks accumulate**: inspect storage credentials, bucket/object identifiers, scheduler state, and the deletion error logs.

Source entry points: [Admin module](../../../Src/Framework/Backend/Alcedo.Framework.Admin/AlcedoAdminModule.cs), [Quartz controller](../../../Src/Framework/Backend/Alcedo.Framework.Admin/Controller/QuartzController.cs), [job registry](../../../Src/Core/Backend/Alcedo.Core.Infrastructure/Job/Registry.cs), and [storage cleaner](../../../Src/Framework/Backend/Alcedo.Framework.Admin/Job/StorageCleaner.cs). Core's [miscellaneous infrastructure guide](../../Core/Backend/02e-Core.Infrastructure-Misc.md) describes the underlying scheduling/storage facilities.
