# Alcedo.Core.Infrastructure — Overview

`Alcedo.Core.Infrastructure` is the implementation layer of Alcedo Core. It bridges the abstract contracts defined in `Abstractions`, `Domain`, and `Application` to concrete third-party libraries and frameworks.

This document lists the external dependencies and infrastructure components used by this layer, serving as a quick reference for what connects to what.

---

## External Dependencies

### Database — PostgreSQL + EF Core

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.EntityFrameworkCore` | 10.0.7  | ORM core |
| `Microsoft.EntityFrameworkCore.Relational` | 10.0.7  | Relational provider infrastructure |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.1  | PostgreSQL EF Core provider |
| `AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL` | 0.5.1   | Quartz job store migrations for PostgreSQL |

**What Alcedo provides on top:**
- `AlcedoDbContext` — minimal DbContext with convention-based configuration
- `AlcedoModelCustomizer` — auto-scans module assemblies for `IEntityTypeConfiguration`
- `AggregateSaveChangesInterceptor` — auto-maintains Version, CreatedAt, UpdatedAt, and collects domain events
- `EntityConfigurationBase<T>` / `AggregateRootConfigurationBase<T>` — snake_case columns, UUID keys, converters
- `AesGcmEncryption` / `AlcedoColumnEncryptor` — column-level AES-GCM encryption

---

### Message Queue — RabbitMQ + MassTransit

| Package | Version | Purpose |
|---------|---------|---------|
| `MassTransit.EntityFrameworkCore` | 8.5.8 | EF Core Outbox pattern support |
| `MassTransit.RabbitMQ` | 8.5.8 | RabbitMQ transport |

**What Alcedo provides on top:**
- `UnitOfWorkExecutor` — wraps business logic in a transaction, publishes collected aggregate events via MassTransit Outbox
- `DomainEventPublisher` — publishes application-level events
- `EventsCollector` — gathers domain events from ChangeTracker after SaveChanges

---

### Cache & Distributed Lock — Redis

| Package | Version | Purpose |
|---------|---------|---------|
| `StackExchange.Redis` | 2.12.14 | Redis client |
| `DistributedLock.Redis` | 1.1.1 | Distributed locking via Redis (Medallion.Threading) |

**What Alcedo provides on top:**
- `DistributedLockFactory` — factory for creating named distributed locks
- `DistributedLockRedisKeySpace` — Redis key namespace for locks
- `IdempotencyKeyRedisKeySpace` — Redis key namespace for idempotency keys
- `RedisvalueExtension` — serialization helpers for Redis values

---

### Job Scheduling — Quartz.NET

| Package | Version | Purpose |
|---------|---------|---------|
| `Quartz` | 3.17.1 | Scheduling engine |
| `Quartz.AspNetCore` | 3.17.1 | ASP.NET Core integration |
| `Quartz.Extensions.Hosting` | 3.17.1 | `IHostedService` integration |
| `Quartz.Serialization.SystemTextJson` | 3.17.1 | `System.Text.Json` serialization for job data |

**What Alcedo provides on top:**
- `AlcedoJobBase` — base class for scheduled jobs
- `JobRegistry` — registry for discovering and scheduling jobs
- Job attributes for declarative scheduling

---

### Logging — Serilog

| Package | Version | Purpose |
|---------|---------|---------|
| `Serilog.AspNetCore` | 10.0.0 | Structured logging integration |

Serilog is configured at the host level. Infrastructure components use `ILogger<T>` via DI.

---

### Email

| Package | Version | Purpose |
|---------|---------|---------|
| `MailKit` | 4.16.0 | SMTP client for sending emails |

Wrapped by `EmailService` in the infrastructure layer.

---

## Internal Project Dependencies

```
Alcedo.Core.Infrastructure
    ├── Alcedo.Core.Abstractions
    ├── Alcedo.Core.Application
    └── Alcedo.Core.Domain
```

`Infrastructure` depends on all three inner layers and implements their contracts. It is referenced by the Host layer but **should not** be referenced by Framework modules directly (modules use abstractions and application contracts).

---

## What's Inside (Quick Map)

| Folder / Area | Key Types | Responsibility |
|---------------|-----------|--------------|
| `EfCore/` | `AlcedoDbContext`, `AlcedoModelCustomizer`, interceptors, encryptors | Database access, model building, conventions |
| `Configuration/` | `EntityConfigurationBase`, `AggregateRootConfigurationBase` | EF Core entity configuration base classes |
| `RepositoryBase/` | `EntityStoreBase`, `AggregateRepositoryBase`, `PessimisticLockQueryBuilder` | Write-side repositories, pessimistic locking |
| `QueryRepositoryBase/` | `EntityQueryRepositoryBase` | Read-side repositories, Mapster projection |
| `SearchQuery/` | `SearchQueryBuilder`, `SearchQueryMetaCache` | Reflection-based search query compilation |
| `ReferenceIntegrityImpl/` | `AggRefChecker`, `AggRefScaner`, `ReferenceIntegrityCompiler` | Cross-aggregate referential integrity |
| `Job/` | `BaseJob`, `JobRegistry`, attributes | Quartz job scheduling |
| `RedisKeySpace/` | `RedisKeySpace`, `DistributedLockRedisKeySpace`, `IdempotencyKeyRedisKeySpace` | Redis key organization |
| Root level | `UnitOfWorkExecutor`, `TransactionManager`, `EventsCollector`, `DomainEventPublisher` | Transaction boundaries, event publishing |

---

## Configuration Required

To use `Alcedo.Core.Infrastructure`, the host application must configure at minimum:

```json
"DataSourceOptions": {
  "RabbitMQ": {
    "Host": "{MqHost}",
    "Port": 5672,
    "VirtualHost": "{VHost:/}",
    "Username": "{MqUser:admin}",
    "Password": "{MqPassword}"
  },
  "Postgres": "Host={DataBaseHost};Database={DataBaseName};Username={DataBaseUser};Password={DataBasePassword}",
  "Redis": "{RedisHost}:{RedisPort:6379},password={RedisPassword},defaultDatabase=0"
},
```

Registration is done via extension methods in `ProgramExtensions/`:

```csharp
builder.Services.AddAlcedoEfCore(configuration);
builder.Services.AddAlcedoMassTransit(configuration, registry.Assemblies);
builder.Services.AddAlcedoRedis(configuration);
builder.Services.AddAlcedoQuartz();
```

---

## Next Documents

This overview is followed by detailed documents for each major area:

- `02a-Core.Infrastructure-EFCore.md` — Database context, configuration, interceptors, encryption
- `02b-Core.Infrastructure-Repository.md` — Write/read repositories, pessimistic locking, search queries
- `02c-Core.Infrastructure-Transaction-Events.md` — Unit of Work, transactions, domain event publishing
- `02d-Core.Infrastructure-Advanced.md` — Reference integrity, distributed locks, Redis, Quartz jobs
