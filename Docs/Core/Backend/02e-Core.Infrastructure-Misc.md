# Alcedo.Core.Infrastructure — Miscellaneous Services

This document covers infrastructure services that do not warrant standalone deep-dive documents: **Redis** (key-space isolation, idempotency, caching utilities) and **Quartz.NET** (job scheduling with auto-discovery).

---

## Table of Contents

- [Redis](#redis)
  - [Key-Space Isolation](#key-space-isolation)
  - [Idempotency Keys](#idempotency-keys)
  - [RedisValue Extensions](#redisvalue-extensions)
- [Quartz.NET Job Scheduling](#quartznet-job-scheduling)
  - [AlcedoJobBase](#alcedojobbase)
  - [QuartzJobAttribute](#quartzjobattribute)
  - [Auto-Discovery](#auto-discovery)
- [Configuration](#configuration)

---

## Redis

Alcedo uses **StackExchange.Redis** for caching, distributed locking, and idempotency. The framework provides a thin key-space isolation layer to prevent key collisions between different concerns.

### Key-Space Isolation

All Redis usage goes through subclasses of `RedisKeySpaceBase`:

```csharp
public abstract class RedisKeySpaceBase
{
    public IDatabase Db { get; }
}
```

The constructor automatically prefixes all keys with `{GlobalPrefix}:{ClassNameWithoutSuffix}:`:

```csharp
// DistributedLockRedisKeySpace → prefix: "Alcedo:DistributedLock:"
// IdempotencyKeyRedisKeySpace → prefix: "Alcedo:IdempotencyKey:"
```

This ensures that `StringSet("foo", ...)` in `DistributedLockRedisKeySpace` writes to `Alcedo:DistributedLock:foo`, while the same key in `IdempotencyKeyRedisKeySpace` writes to `Alcedo:IdempotencyKey:foo`.

**Existing key spaces:**

| Class | Purpose |
|-------|---------|
| `DistributedLockRedisKeySpace` | Backing store for Medallion distributed locks |
| `IdempotencyKeyRedisKeySpace` | Idempotency token storage |

**Adding a new key space:**

```csharp
public sealed class MyFeatureRedisKeySpace : RedisKeySpaceBase
{
    public MyFeatureRedisKeySpace(IConnectionMultiplexer m, IOptions<RedisOptions> o) : base(m, o) { }
}
```

The class is auto-discovered and registered as a singleton by `AddAlcedoRedis`. Naming must end with `RedisKeySpace`.

### Idempotency Keys

The framework provides three idempotency strategies via `IIdempotencyKeyProvider`:

| Strategy | Behavior | Use Case |
|----------|----------|----------|
| **Strict** | Server generates a token; client must present it exactly once; consumed on use | High-security endpoints (payments, transfers) |
| **Soft** | Client provides a token; deduplicated within a time window | General write deduplication |
| **Content-based** | Hash of request body used as token; deduplicated within a time window | Replay-safe mutations |

**Strict flow:**

```csharp
// 1. Client requests a token
var token = await _idempotencyProvider.GenerateStrictIdempotencyKeyAsync("POST:/payments", expiryMinutes: 10);

// 2. Client sends request with X-Idempotency-Key: {token}
// 3. Server validates — token is deleted atomically on success
//    Second request with same token fails
```

**Soft flow:**

```csharp
// Client generates its own token (e.g. UUID)
// Server stores token in Redis with NX (set if not exists)
// Duplicate within expiry window is rejected
```

**Filter attributes:**

```csharp
[SoftIdempotencyKey]
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder(...) { }

[StrictIdempotencyKey]
[OverrideIdempotencyKeyExpiryMinutes(30)]
[HttpPost("payments")]
public async Task<IActionResult> ProcessPayment(...) { }

[IgnoreIdempotencyKey]
[HttpPost("webhooks")]
public async Task<IActionResult> ReceiveWebhook(...) { }
```

The `RequestIdempotencyKeyFilter` action filter enforces these rules:
- `GET` requests skip idempotency checks
- `PUT`/`PATCH`/`DELETE` default to strict mode only if explicitly annotated
- The token is combined with the user ID (`{userId}:{rawToken}`) so tokens are scoped per user

### RedisValue Extensions

Helper extensions for `StackExchange.Redis.RedisValue`:

```csharp
public static class RedisValueExtensions
{
    public static byte[]? ToByteArray(this RedisValue value);
    public static Guid? ToGuid(this RedisValue value);
}
```

These resolve .NET 10 `JsonSerializer` overload ambiguity when deserializing Redis values.

---

## Quartz.NET Job Scheduling

Alcedo uses **Quartz.NET** with a PostgreSQL persistent job store for background tasks. Jobs are auto-discovered from module assemblies via attribute scanning.

### AlcedoJobBase

All jobs should inherit from `AlcedoJobBase`:

```csharp
public abstract class AlcedoJobBase : IJob
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ILogger Logger;

    protected abstract Task ExecuteAsync(
        string jobName,
        IDictionary<string, object> data,
        IServiceProvider sp,
        CancellationToken ct);
}
```

The base class:
- Creates a **dependency injection scope** per execution
- Passes job data map and cancellation token to `ExecuteAsync`
- Provides a typed `Logger` for the concrete job class

**Example:**

```csharp
[QuartzJob("0 0 2 * * ?", "DailyReportJob", "Reporting")]
public class DailyReportJob : AlcedoJobBase
{
    public DailyReportJob(IServiceProvider sp, ILoggerFactory lf) : base(sp, lf) { }

    protected override async Task ExecuteAsync(string jobName, IDictionary<string, object> data,
        IServiceProvider sp, CancellationToken ct)
    {
        var repository = sp.GetRequiredService<IReportRepository>();
        await repository.GenerateDailyReportAsync(ct);
    }
}
```

### QuartzJobAttribute

Marks a class as a Quartz job and defines its schedule:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class QuartzJobAttribute : Attribute
{
    public string? CronExpression { get; set; }
    public int? IntervalInSeconds { get; set; }
    public string? JobName { get; set; }
    public string? JobGroup { get; set; }
}
```

| Schedule Type | Constructor | Example |
|---------------|-------------|---------|
| Cron | `[QuartzJob(cron, name, group)]` | `[QuartzJob("0 0 2 * * ?", "NightlyCleanup")]` — daily at 2 AM |
| Interval | `[QuartzJob(intervalSecs, name, group)]` | `[QuartzJob(300, "Heartbeat")]` — every 5 minutes |

### Auto-Discovery

During host startup (`AddAutoQuartzJobs`), all non-abstract classes with `[QuartzJob]` are scanned from module assemblies and registered automatically:

```csharp
services.AddAutoQuartzJobs(configuration, registry.Assemblies);
```

This configures:
- PostgreSQL persistent store (table prefix `quartz.QRTZ_`)
- `System.Text.Json` serializer for job data
- `IgnoreDuplicates = true` (duplicate job definitions are skipped)
- `OverWriteExistingData = false` (existing schedules are preserved)
- `InterruptJobsOnShutdown = true`
- `WaitForJobsToComplete = true` on the hosted service

---

## Configuration

Miscellaneous services share configuration sections:

```json
{
  "DataSource": {
    "Postgres": "Host=localhost;Database=alcedo;Username=...;Password=...",
    "Redis": "localhost:6379,password=,defaultDatabase=0"
  },
  "RedisOptions": {
    "GlobalPrefix": "Alcedo"
  },
  "IdempotencyKeyOptions": {
    "StrictKeyExpireMinutes": 10,
    "CommonKeyExpireMinutes": 1
  }
}
```

| Section | Purpose |
|---------|---------|
| `DataSource:Redis` | StackExchange.Redis connection string |
| `RedisOptions:GlobalPrefix` | Prefix for all framework-managed Redis keys |
| `IdempotencyKeyOptions` | TTL defaults for strict and soft idempotency keys |
