# Alcedo.Core Overview

Alcedo.Core is the foundational framework layer of the Alcedo modular monolith. It provides the building blocks for DDD, CQRS, event-driven architecture, and modular deployment — without prescribing business logic.

---

## Architecture Layers

Alcedo.Core follows **strict inward dependencies**. Each layer can only depend on layers closer to the center. Infrastructure is special — it implements interfaces defined by the inner layers, so it depends on all of them.

```
                     ┌────────────────────────────────┐
                     │            Host                │
                     │ (coordinates / references all) │
                     └────────────────────────────────┘
                                     │
        ┌────────────────────────────│
        │                            │                  
        ▼                            │           
   ┌─────────┐                       │
   │  Pre    │                       │
   └────┬────┘                       │
        │                            │
        │ depends on                 │
        ▼                            ▼
   ┌─────────────┐  implements ┌─────────────┐
   │ Application │◄────────────│    Infra    │
   └──────┬──────┘             └──┬─────┬────┘
          │                       │     │
          │ depends on            │     │
          ▼                       │     │
   ┌─────────────┐                │     │
   │   Domain    │◄───────────────┘     │
   └──────┬──────┘     implements       │
          │                             │
          │ depends on                  │ depends on
          ▼                             │
   ┌─────────────┐                      │
   │Abstractions │◄─────────────────────┘
   └─────────────┘
```

**Dependency rule:** All arrows point inward. Host coordinates registration but does not contain business logic.

| Layer | Responsibility | Key Concepts |
|-------|---------------|--------------|
| **Abstractions** | Zero-dependency primitives | `ValueAtom`, `EventRoot`, `AlcedoException`, `[RegisterAs]` |
| **Domain** | Business model and rules | `AggregateRoot`, `DomainEventBase`, state machines, repository contracts |
| **Application** | Read contracts and DTOs | `EntityDtoBase`, `EntitySearchQueryBase`, `IEntityQueryRepository` |
| **Infrastructure** | Persistence, messaging, locking | EF Core, MassTransit, Redis, Quartz, `UnitOfWorkExecutor` |
| **Presentation** | HTTP-facing infrastructure | Controllers, exception handler, model binders, API versioning |
| **Host** | Bootstrap and wiring | `AlcedoBuilder`, middleware pipeline, Swagger, health checks |

---

## What Alcedo.Core Provides

### Modular Monolith

- `IAlcedoModule` contract for self-registering business modules
- Auto-DI scanning via `[RegisterAs]` and `[RegisterSelf]`
- Per-module database schema assignment
- Assembly scanning for EF configurations, Mapster profiles, and Quartz jobs

### CQRS

- **Write side:** `EntityStoreBase` loads entities by ID only (no queries)
- **Read side:** `EntityQueryRepositoryBase` projects entities to DTOs via Mapster (no tracking)
- `SearchQueryBuilder` constructs filter expressions from attributed query DTOs
- `PessimisticLockQueryBuilder` provides raw SQL `FOR UPDATE` / `FOR SHARE`

### DDD Primitives

- `EntityBase` with UUID v7 primary keys
- `AggregateRoot` with versioning, audit timestamps, and domain events
- `ValueAtom` value objects with JSON and EF Core support
- State machines (`StateFlowBase`, `StateActionGuardBase`) for lifecycle modeling

### Event-Driven Architecture

- Domain events collected automatically at Unit of Work commit
- MassTransit + EF Core Outbox for reliable message publishing
- `DomainEventPublisher` for out-of-band event publishing
- `ApplicationEventBase` for fire-and-forget notifications

### Cross-Cutting Concerns

- **Reference Integrity:** Expression-tree compiled cross-aggregate reference checks
- **Distributed Locks:** Redis-backed reader-writer locks with batch acquisition and deadlock prevention
- **Idempotency:** Strict, soft, and content-based idempotency keys via Redis
- **Global Exception Handling:** Unified `ProblemDetails` responses with trace IDs and error codes
- **API Versioning:** URL segment, header, and query string version readers

---

## Document Map

| Document | Layer | Topics |
|----------|-------|--------|
| [`01-Alcedo.Core.Abstractions.md`](01-Alcedo.Core.Abstractions.md) | Abstractions | Module system, DI auto-registration, exceptions, `ValueAtom`, `EventRoot` |
| [`02a-Core.Infrastructure-EFCore.md`](02a-Core.Infrastructure-EFCore.md) | Infrastructure | DbContext, model customizer, interceptors, column encryption, conventions |
| [`02b-Core.Infrastructure-Repository.md`](02b-Core.Infrastructure-Repository.md) | Infrastructure | `EntityStoreBase`, `EntityQueryRepositoryBase`, `SearchQueryBuilder`, pessimistic locking |
| [`02c-Core.Infrastructure-Transaction-Events.md`](02c-Core.Infrastructure-Transaction-Events.md) | Infrastructure | `UnitOfWorkExecutor`, `TransactionManager`, event collection, outbox |
| [`02d-Core.Infrastructure-Advanced.md`](02d-Core.Infrastructure-Advanced.md) | Infrastructure | Reference integrity, distributed locks |
| [`02e-Core.Infrastructure-Misc.md`](02e-Core.Infrastructure-Misc.md) | Infrastructure | Redis key spaces, idempotency, Quartz scheduling |
| [`03-Alcedo.Core.Domain.md`](03-Alcedo.Core.Domain.md) | Domain | Entities, repository contracts, domain events, exceptions, metadata |
| [`03a-Core.Domain-StateMachine.md`](03a-Core.Domain-StateMachine.md) | Domain | `StateFlowBase`, `StateActionGuardBase`, master data state machine |
| [`04-Alcedo.Core.Application.md`](04-Alcedo.Core.Application.md) | Application | DTOs, search queries, read contracts, components, exceptions |
| [`05-Alcedo.Core.Presentation.md`](05-Alcedo.Core.Presentation.md) | Presentation | Controllers, exception handler, model binders, JSON converters, versioning |
| [`06-Alcedo.Core.Host.md`](06-Alcedo.Core.Host.md) | Host | Bootstrap, service registration, middleware pipeline, Swagger, health checks, CORS |

---

## Design Philosophy

1. **Composition over inheritance.** Application services compose focused components (search, edit, status) rather than inheriting monolithic base classes.

2. **Convention over configuration.** Snake-case naming, plural table names, UUID primary keys, and UTC timestamps are defaults — but every convention has an override point.

3. **Performance by design.** Reflection is pushed to startup and cached. `SearchQueryMetaCache`, `ReferenceIntegrityCompiler`, and `AggregateRoot Graph Center` all use compiled expression trees.

4. **Explicit over implicit.** Pessimistic locks, transaction boundaries, and event publishing are all explicit in code. There is no ambient transaction or automatic event dispatch.

5. **Fail fast at boundaries.** Model binding, idempotency, and validation throw presentation-layer exceptions immediately. Business rules throw domain exceptions. Infrastructure concerns (locks, references) are checked before mutations.
