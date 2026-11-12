# Alcedo.Core.Infrastructure — EF Core

This document covers the database access layer of Alcedo Core, including the DbContext, model customization, entity configuration base classes, interceptors, column encryption, and the aggregate root graph center.

---

## Table of Contents

- [AlcedoDbContext](#alcedodbcontext)
- [AlcedoModelCustomizer](#alcedomodelcustomizer)
- [AlcedoOptionsExtension](#alcedooptionsextension)
- [Entity Configuration](#entity-configuration)
  - [EntityConfigurationBase](#entityconfigurationbase)
  - [AggregateRootConfigurationBase](#aggregaterootconfigurationbase)
- [SaveChanges Interceptor](#savechanges-interceptor)
- [Aggregate Root Graph Center](#aggregate-root-graph-center)
- [Column Encryption](#column-encryption)
- [ValueAtom Converter](#valueatom-converter)
- [Registration](#registration)

---

## AlcedoDbContext

`AlcedoDbContext` is intentionally minimal. It contains no `DbSet` properties and no `OnModelCreating` override. All entity discovery and configuration happens externally via `AlcedoModelCustomizer`.

```csharp
public sealed class AlcedoDbContext : DbContext
{
    public AlcedoDbContext(DbContextOptions<AlcedoDbContext> options) : base(options) { }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>()
            .HavePrecision(18, 6);

        configurationBuilder.Properties<string>()
            .HaveMaxLength(255);

        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();
    }
}
```

**Global conventions applied:**

| Type | Convention |
|------|-----------|
| `decimal` | Precision `18, 6` |
| `string` | Max length `255` |
| `DateTime` | Stored and retrieved as UTC |

---

## AlcedoModelCustomizer

This customizer replaces EF Core's default `RelationalModelCustomizer`. It is responsible for:

1. Registering MassTransit Outbox and Quartz infrastructure tables
2. Scanning module assemblies for `IEntityTypeConfiguration`
3. Auto-assigning database schemas per module

```csharp
internal class AlcedoModelCustomizer : RelationalModelCustomizer
{
    public override void Customize(ModelBuilder builder, DbContext context)
    {
        base.Customize(builder, context);

        ConfigureInfrastructure(builder);

        // Core own configurations
        builder.ApplyConfigurationsFromAssembly(typeof(AlcedoDbContext).Assembly);

        // Module configurations
        foreach (var module in _alcedoModules)
        {
            if (module.Assembly == typeof(AlcedoDbContext).Assembly)
                continue;

            builder.ApplyConfigurationsFromAssembly(module.Assembly);

            // Auto-assign schema for entities without explicit schema
            var moduleEntities = builder.Model.GetEntityTypes()
                .Where(e => e.ClrType.Assembly == module.Assembly && e.GetSchema() == null);

            foreach (var entityType in moduleEntities)
            {
                entityType.SetSchema(module.ModuleCode);
            }
        }
    }
}
```

**Schema assignment rules:**
- If an entity configuration explicitly sets a schema (e.g. `builder.ToTable("users", "identity")`), that schema is preserved.
- If no schema is set, the entity's module name is used as the schema.
- Infrastructure tables (MassTransit, Quartz) use their own fixed schemas.

**Infrastructure tables registered:**

| Table | Schema        | Purpose |
|-------|---------------|---------|
| `inbox_state` | `masstransit` | MassTransit inbox deduplication |
| `outbox_message` | `masstransit` | MassTransit outbox messages |
| `outbox_state` | `masstransit` | MassTransit outbox state |
| Quartz tables | `quartz`      | Job store (PostgreSQL provider) |

---

## AlcedoOptionsExtension

A custom `IDbContextOptionsExtension` that carries the module list into the DbContext pipeline. This allows `AlcedoModelCustomizer` to access registered modules during model building.

```csharp
public class AlcedoOptionsExtension : IDbContextOptionsExtension
{
    public IReadOnlyList<IAlcedoModule> Modules { get; }

    public void ApplyServices(IServiceCollection services)
    {
        services.AddSingleton(Modules);
    }
}
```

This extension is added internally by `AddAlcedoCoreDbContext`. You do not use it directly.

---

## Entity Configuration

### EntityConfigurationBase

The base class for all entity configurations. Provides opinionated defaults and extension points.

Configuration is split into two dimensions:

- **Vertical (default) configuration** — handled by `IEntityTypeDefaultConfigurator<T>`. This captures framework-level defaults that travel down the entity inheritance chain (e.g. aggregate root versioning, reference data code indexes, master data status converters). Framework base classes override `DefaultConfigurator` to inject their defaults.
- **Horizontal (business) configuration** — handled by `virtual` hook methods (`ConfigureProperties`, `ConfigureIndexes`, etc.). Business entity configurations override these to add their own mappings. No `base` call is required.

```csharp
public interface IEntityTypeDefaultConfigurator<T> where T : EntityBase
{
    void Apply(EntityTypeBuilder<T> builder);
}

public class EntityTypeDefaultConfigurator<T> : IEntityTypeDefaultConfigurator<T>
    where T : EntityBase
{
    public virtual void Apply(EntityTypeBuilder<T> builder) { }
}

public abstract class EntityConfigurationBase<T> : IEntityTypeConfiguration<T>
    where T : EntityBase
{
    protected virtual string TableName => typeof(T).Name.Pluralize().Underscore();
    protected virtual string? Schema => null;

    protected virtual IEntityTypeDefaultConfigurator<T> DefaultConfigurator
        => new EntityTypeDefaultConfigurator<T>();

    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        ConfigureTable(builder);
        ConfigureKey(builder);
        ApplyDefaultSnakeCaseColumnNames(builder);
        DefaultConfigurator.Apply(builder);

        // Horizontal hooks
        ConfigureProperties(builder);
        ConfigureIndexes(builder);
        ConfigureRelationships(builder);
        ConfigureQueryFilters(builder);
        ConfigureConverters(builder);
        SeedData(builder);
    }
}
```

**Default conventions applied by the base class:**

| Convention | Value |
|------------|-------|
| Table name | `Humanizer` pluralize + underscore (e.g. `UserAgg` → `user_aggs`, `Category` → `categories`) |
| Primary key | `Id`, `ValueGeneratedNever()` (UUID v7 assigned by constructor) |
| Column names | `Humanizer.Underscore()` (e.g. `CreatedAt` → `created_at`, `EmailAddress` → `email_address`) |

**Horizontal override hooks** (business configurations override these, no `base` call needed):

| Method | Purpose |
|--------|---------|
| `ConfigureProperties` | Column definitions, max length, precision |
| `ConfigureIndexes` | Database indexes |
| `ConfigureRelationships` | Foreign keys, navigations |
| `ConfigureQueryFilters` | Global query filters |
| `ConfigureConverters` | Value converters |
| `SeedData` | Initial data seeding |

**pg_trgm index support**

Alcedo provides built-in support for PostgreSQL `pg_trgm` (trigram) indexes to accelerate `LIKE` / `ILIKE` queries.

The underlying extension method is provided by `EntityTypeBuilderExtensions`:

```csharp
public enum TrgmIndexType { Gin, Gist }

public static IndexBuilder<TEntity> HasTrgmIndex<TEntity>(
    this EntityTypeBuilder<TEntity> builder,
    Expression<Func<TEntity, object?>> expr,
    string indexName,
    TrgmIndexType method = TrgmIndexType.Gin)
{
    return builder
        .HasIndex(expr)
        .HasMethod(method.ToString().ToLowerInvariant())   // "gin" or "gist"
        .HasOperators(method == TrgmIndexType.Gin ? "gin_trgm_ops" : "gist_trgm_ops")
        .HasDatabaseName(indexName);
}
```

`EntityConfigurationBase` wraps this with a convenience method that auto-generates the index name:

```csharp
protected IndexBuilder<T> HasTrgmIndex(
    EntityTypeBuilder<T> builder,
    Expression<Func<T, object?>> expr,
    TrgmIndexType method = TrgmIndexType.Gin)
```

Auto-generated index name format: `ix_{table_name}_{column_name}_{method}_trgm` (lower-cased).

**Example:**

```csharp
protected override void ConfigureIndexes(EntityTypeBuilder<ProductAgg> builder)
{
    // Auto-generates index name ix_product_aggs_name_gin_trgm
    HasTrgmIndex(builder, e => e.Name, TrgmIndexType.Gin);

    // Or specify the index name manually
    builder.HasTrgmIndex(e => e.Description, "ix_products_desc_trgm", TrgmIndexType.Gist);
}
```

| Method | Best for |
|--------|----------|
| `Gin` (default) | Read-heavy workloads; faster queries |
| `Gist` | Write-heavy workloads; or when `Gist`-specific operator classes are needed |

**Example:**

```csharp
internal class UserAggregateConfiguration : MasterDataConfigurationBase<UserAgg>
{
    protected override void ConfigureIndexes(EntityTypeBuilder<UserAgg> builder)
    {
        builder.HasIndex(u => u.LoginName).IsUnique();
        builder.HasIndex(u => u.IsRootUser)
            .IsUnique()
            .HasFilter("is_root_user = true");
    }

    protected override void ConfigureRelationships(EntityTypeBuilder<UserAgg> builder)
    {
        builder.HasMany(u => u.UserRoles)
            .WithOne()
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    protected override void ConfigureConverters(EntityTypeBuilder<UserAgg> builder)
    {
        builder.Property(u => u.TwoFactorSecret)
            .HasConversion(AlcedoColumnEncryptor.Converter);
    }
}
```

### AggregateRootConfigurationBase

Extends `EntityConfigurationBase` for aggregate roots. Adds optimistic concurrency token support via the default configurator chain.

```csharp
public class AggregateRootTypeDefaultConfigurator<T> : EntityTypeDefaultConfigurator<T>
    where T : AggregateRoot
{
    public override void Apply(EntityTypeBuilder<T> builder)
    {
        base.Apply(builder);
        builder.Property(e => e.Version).IsConcurrencyToken();
    }
}

public abstract class AggregateRootConfigurationBase<T> : EntityConfigurationBase<T>
    where T : AggregateRoot
{
    protected override IEntityTypeDefaultConfigurator<T>
        DefaultConfigurator => new AggregateRootTypeDefaultConfigurator<T>();
}
```

All aggregate root configurations should inherit from this class to get automatic `Version` concurrency token setup.

The same pattern continues down the framework hierarchy:

| Base Class | Default Configurator | What it adds |
|------------|---------------------|--------------|
| `EntityConfigurationBase<T>` | `EntityTypeDefaultConfigurator<T>` | (empty) |
| `AggregateRootConfigurationBase<T>` | `AggregateRootTypeDefaultConfigurator<T>` | `Version` as concurrency token |
| `ReferenceDataConfigurationBase<T>` | `ReferenceDataTypeDefaultConfigurator<T>` | Unique index on `Code` |
| `MasterDataConfigurationBase<T>` | `MasterDataTypeDefaultConfigurator<T>` | `Status` value converter |
| `MasterDataLogConfigurationBase<T>` | `MasterDataLogTypeDefaultConfigurator<T>` | `Note` as `text` column |

---

## SaveChanges Interceptor

### AggregateSaveChangesInterceptor

An `ISaveChangesInterceptor` that automatically manages aggregate metadata before changes are persisted.

**What it does:**

1. **Phase One** — For every `AggregateRoot` in the `ChangeTracker`:
   - `Added` → calls `TouchForCreate()` (sets `Version = 1`, `CreatedAt`, `UpdatedAt`)
   - `Modified` → calls `TouchForUpdate()` (increments `Version`, updates `UpdatedAt`)
   - `Unchanged` with `ForceModified = true` → marks as modified and touches
   - `Unchanged` but has modified descendants (via navigation traversal) → marks root as modified and touches

2. **Phase Two** — For deleted child entities detached from their parent navigation:
   - Walks up the foreign key chain via `AlcedoAggregateRootGraphCenter`
   - Locates the owning aggregate root
   - Applies `TouchForUpdate()` if the root is unchanged

```csharp
// ForceModified is used when you mutate a computed/derived property
// that EF Core cannot detect as a change
agg.MarkAsModified();
await _unitOfWork.ExecuteAsync(() => { /* ... */ });
```

**Registration:**

```csharp
services.AddSingleton<AggregateSaveChangesInterceptor>();
services.AddScoped<IInterceptor, AggregateSaveChangesInterceptor>();
```

The interceptor is automatically added to the `AlcedoDbContext` via `AddAlcedoCoreDbContext`.

---

## Aggregate Root Graph Center

### AlcedoAggregateRootGraphCenter

Builds an in-memory graph of aggregate root hierarchies at application startup. This graph maps:
- Which entities belong to which aggregate root
- Parent-child relationships via foreign keys
- Navigation paths

**Purpose:**
- Phase Two of `AggregateSaveChangesInterceptor` uses this graph to find the owning aggregate root of a deleted child entity
- Validates that no entity belongs to multiple aggregates
- Detects cycles in aggregate hierarchies

**Initialization:**

```csharp
app.UseAlcedoAggregateRootGraphCenter(); // called during pipeline setup
```

This triggers a one-time scan of the EF Core model and builds the graph. The graph is then cached for the lifetime of the application.

---

## Column Encryption

### AlcedoColumnEncryptor

Provides transparent AES-GCM column encryption for EF Core string properties.

```csharp
// In your entity configuration
builder.Property(e => e.SecretField)
    .HasConversion(AlcedoColumnEncryptor.Converter);
```

**Initialization:**

The encryption key is loaded from configuration during host startup:

```csharp
AlcedoColumnEncryptor.Initialize(configuration["AlcedoColumnEncryptorOptions:Key"]);
```

This is done automatically by `AddAlcedoCoreDbContext`.

**Important:**
- The key must be a Base64-encoded 256-bit AES key.
- Encrypted values include a 12-byte nonce and 16-byte auth tag prepended to the ciphertext.
- Encrypted columns should use `text` or large `varchar` types, not `varchar(255)`.

### AesGcmEncryption

The underlying AES-GCM implementation using `System.Security.Cryptography.AesGcm`.

---

## ValueAtom Converter

### ValueAtomConverter

EF Core value converter for all `ValueAtom` subclasses. Stores the underlying `Value` string in the database and reconstructs the correct subclass on read.

```csharp
// In your entity configuration
builder.Property(e => e.Status)
    .HasConversion<ValueAtomConverter<MasterDataStateValue>>()
    .HasMaxLength(50);
```

**How it works:**
- Write: `MasterDataStateValue("Enabled")` → `"Enabled"`
- Read: `"Enabled"` → `new MasterDataStateValue("Enabled")`

The converter uses `Activator.CreateInstance` to reconstruct the subclass. The generic parameter `TImpl` must be a **concrete, non-abstract** `ValueAtom` subclass.

---

## Registration

Register EF Core infrastructure in the host:

```csharp
builder.Services.AddAlcedoCoreDbContext(configuration, registry.Modules);
```

This extension method:
1. Initializes `AlcedoColumnEncryptor` with the encryption key from config
2. Registers `AggregateSaveChangesInterceptor` as singleton + scoped `IInterceptor`
3. Configures Npgsql with `AlcedoDbContext`'s assembly as the migrations assembly
4. Replaces `IModelCustomizer` with `AlcedoModelCustomizer`
5. Injects the module list via `AlcedoOptionsExtension`
6. Adds all registered `IInterceptor` services to the DbContext

**Required configuration:**

```json
{
  "DataSource": {
    "Postgres": "Host=localhost;Database=alcedo;Username=...;Password=..."
  },
  "AlcedoColumnEncryptorOptions": {
    "Key": "base64-encoded-256-bit-key"
  }
}
```
