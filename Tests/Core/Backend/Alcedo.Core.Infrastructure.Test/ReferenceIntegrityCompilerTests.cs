using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Core.Infrastructure.ReferenceIntegrityImpl;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Alcedo.Core.Infrastructure.Test;

public class RefCompilerTestEntity : EntityBase
{
    public Guid OwnerId { get; set; }
    public Guid? NullableOwnerId { get; set; }
}

public class RefCompilerParentEntity : EntityBase
{
    public string Name { get; set; } = "";
    public RefCompilerChildEntity? Child { get; set; }
}

public class RefCompilerChildEntity : EntityBase
{
    public Guid Value { get; set; }
}

public class ReferenceIntegrityCompilerTests
{
    private static AlcedoDbContext CreateInMemoryContext()
    {
        var model = BuildModelWithEntity<RefCompilerTestEntity>();

        var options = new DbContextOptionsBuilder<AlcedoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseModel(model)
            .Options;
        return new AlcedoDbContext(options);
    }

    private static IModel BuildModelWithEntity<TEntity>() where TEntity : class
    {
        var builder = new DbContextOptionsBuilder<DbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString());
        using var ctx = new ModelBuildingContext(builder.Options, typeof(TEntity));
        return ctx.Model;
    }

    private class ModelBuildingContext : DbContext
    {
        private readonly Type _entityType;

        public ModelBuildingContext(DbContextOptions options, Type entityType) : base(options) =>
            _entityType = entityType;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity(_entityType);
        }
    }

    [Fact]
    public async Task BuildCheckReferenceFunc_NoProperties_ReturnsFalse()
    {
        var func = ReferenceIntegrityCompiler.BuildCheckReferenceFunc(typeof(RefCompilerTestEntity), []);
        await using var ctx = CreateInMemoryContext();

        var result = await func(ctx, Guid.NewGuid(), default);

        Assert.False(result);
    }

    [Fact]
    public async Task BuildCheckReferenceFunc_SingleProperty_Matches()
    {
        var func = ReferenceIntegrityCompiler.BuildCheckReferenceFunc(typeof(RefCompilerTestEntity), ["OwnerId"]);
        await using var ctx = CreateInMemoryContext();
        var targetId = Guid.NewGuid();
        ctx.Set<RefCompilerTestEntity>().Add(new RefCompilerTestEntity { OwnerId = targetId });
        await ctx.SaveChangesAsync();

        var result = await func(ctx, targetId, default);

        Assert.True(result);
    }

    [Fact]
    public async Task BuildCheckReferenceFunc_SingleProperty_NoMatch()
    {
        var func = ReferenceIntegrityCompiler.BuildCheckReferenceFunc(typeof(RefCompilerTestEntity), ["OwnerId"]);
        await using var ctx = CreateInMemoryContext();
        ctx.Set<RefCompilerTestEntity>().Add(new RefCompilerTestEntity { OwnerId = Guid.NewGuid() });
        await ctx.SaveChangesAsync();

        var result = await func(ctx, Guid.NewGuid(), default);

        Assert.False(result);
    }

    [Fact]
    public async Task BuildCheckReferenceFunc_MultipleProperties_OrLogic()
    {
        var func = ReferenceIntegrityCompiler.BuildCheckReferenceFunc(typeof(RefCompilerTestEntity),
            ["OwnerId", "NullableOwnerId"]);
        await using var ctx = CreateInMemoryContext();
        var targetId = Guid.NewGuid();
        ctx.Set<RefCompilerTestEntity>().Add(new RefCompilerTestEntity { NullableOwnerId = targetId });
        await ctx.SaveChangesAsync();

        var result = await func(ctx, targetId, default);

        Assert.True(result);
    }

    [Fact]
    public async Task BuildCheckReferenceFunc_NullableProperty_UsesNullableType()
    {
        var func = ReferenceIntegrityCompiler.BuildCheckReferenceFunc(typeof(RefCompilerTestEntity),
            ["NullableOwnerId"]);
        await using var ctx = CreateInMemoryContext();
        var targetId = Guid.NewGuid();
        ctx.Set<RefCompilerTestEntity>().Add(new RefCompilerTestEntity { NullableOwnerId = targetId });
        await ctx.SaveChangesAsync();

        var result = await func(ctx, targetId, default);

        Assert.True(result);
    }

    [Fact]
    public async Task MergeCheckFuncs_Empty_ReturnsFalse()
    {
        var merged = ReferenceIntegrityCompiler.MergeCheckFuncs([]);

        var result = await merged(null!, Guid.Empty, default);

        Assert.False(result);
    }

    [Fact]
    public async Task MergeCheckFuncs_FirstTrue_ShortCircuits()
    {
        var calls = 0;
        Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>> f1 = (_, _, _) =>
        {
            calls++;
            return Task.FromResult(true);
        };
        Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>> f2 = (_, _, _) =>
        {
            calls++;
            return Task.FromResult(true);
        };

        var merged = ReferenceIntegrityCompiler.MergeCheckFuncs([f1, f2]);
        var result = await merged(null!, Guid.Empty, default);

        Assert.True(result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task MergeCheckFuncs_AllFalse_ReturnsFalse()
    {
        Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>> f1 = (_, _, _) => Task.FromResult(false);
        Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>> f2 = (_, _, _) => Task.FromResult(false);

        var merged = ReferenceIntegrityCompiler.MergeCheckFuncs([f1, f2]);
        var result = await merged(null!, Guid.Empty, default);

        Assert.False(result);
    }

    [Fact]
    public void CompileNavGetter_GetsPropertyValue()
    {
        var getter =
            ReferenceIntegrityCompiler.CompileNavGetter(typeof(RefCompilerParentEntity),
                nameof(RefCompilerParentEntity.Name));
        var obj = new RefCompilerParentEntity { Name = "Test" };

        var result = getter(obj);

        Assert.Equal("Test", result);
    }

    [Fact]
    public void CompileNavGetter_NavigationProperty_ReturnsObject()
    {
        var getter = ReferenceIntegrityCompiler.CompileNavGetter(typeof(RefCompilerParentEntity),
            nameof(RefCompilerParentEntity.Child));
        var child = new RefCompilerChildEntity();
        var obj = new RefCompilerParentEntity { Child = child };

        var result = getter(obj);

        Assert.Same(child, result);
    }

    [Fact]
    public void CompileNavGetter_NullProperty_ReturnsNull()
    {
        var getter = ReferenceIntegrityCompiler.CompileNavGetter(typeof(RefCompilerParentEntity),
            nameof(RefCompilerParentEntity.Child));
        var obj = new RefCompilerParentEntity { Child = null };

        var result = getter(obj);

        Assert.Null(result);
    }

    [Fact]
    public void CompileIdExtractor_NonNullableGuid_AddsToDict()
    {
        var id = Guid.NewGuid();
        var configs = new[]
        {
            new ReferenceConfiguration(
                typeof(AggregateRoot), typeof(RefCompilerTestEntity), typeof(AggregateRoot),
                nameof(RefCompilerTestEntity.OwnerId), ReferenceMode.Strict, [])
        };
        var extractor = ReferenceIntegrityCompiler.CompileIdExtractor(typeof(RefCompilerTestEntity), configs);
        var entity = new RefCompilerTestEntity { OwnerId = id };
        var dict = new Dictionary<Type, HashSet<Guid>>();

        extractor(entity, dict);

        Assert.Single(dict);
        Assert.Contains(id, dict[typeof(AggregateRoot)]);
    }

    [Fact]
    public void CompileIdExtractor_NonNullableGuid_EmptySkipped()
    {
        var configs = new[]
        {
            new ReferenceConfiguration(
                typeof(AggregateRoot), typeof(RefCompilerTestEntity), typeof(AggregateRoot),
                nameof(RefCompilerTestEntity.OwnerId), ReferenceMode.Strict, [])
        };
        var extractor = ReferenceIntegrityCompiler.CompileIdExtractor(typeof(RefCompilerTestEntity), configs);
        var entity = new RefCompilerTestEntity { OwnerId = Guid.Empty };
        var dict = new Dictionary<Type, HashSet<Guid>>();

        extractor(entity, dict);

        Assert.Empty(dict);
    }

    [Fact]
    public void CompileIdExtractor_NullableGuid_NullSkipped()
    {
        var configs = new[]
        {
            new ReferenceConfiguration(
                typeof(AggregateRoot), typeof(RefCompilerTestEntity), typeof(AggregateRoot),
                nameof(RefCompilerTestEntity.NullableOwnerId), ReferenceMode.Strict, [])
        };
        var extractor = ReferenceIntegrityCompiler.CompileIdExtractor(typeof(RefCompilerTestEntity), configs);
        var entity = new RefCompilerTestEntity { NullableOwnerId = null };
        var dict = new Dictionary<Type, HashSet<Guid>>();

        extractor(entity, dict);

        Assert.Empty(dict);
    }

    [Fact]
    public void CompileIdExtractor_NullableGuid_EmptySkipped()
    {
        var configs = new[]
        {
            new ReferenceConfiguration(
                typeof(AggregateRoot), typeof(RefCompilerTestEntity), typeof(AggregateRoot),
                nameof(RefCompilerTestEntity.NullableOwnerId), ReferenceMode.Strict, [])
        };
        var extractor = ReferenceIntegrityCompiler.CompileIdExtractor(typeof(RefCompilerTestEntity), configs);
        var entity = new RefCompilerTestEntity { NullableOwnerId = Guid.Empty };
        var dict = new Dictionary<Type, HashSet<Guid>>();

        extractor(entity, dict);

        Assert.Empty(dict);
    }

    [Fact]
    public void CompileIdExtractor_NullableGuid_WithValue_AddsToDict()
    {
        var id = Guid.NewGuid();
        var configs = new[]
        {
            new ReferenceConfiguration(
                typeof(AggregateRoot), typeof(RefCompilerTestEntity), typeof(AggregateRoot),
                nameof(RefCompilerTestEntity.NullableOwnerId), ReferenceMode.Strict, [])
        };
        var extractor = ReferenceIntegrityCompiler.CompileIdExtractor(typeof(RefCompilerTestEntity), configs);
        var entity = new RefCompilerTestEntity { NullableOwnerId = id };
        var dict = new Dictionary<Type, HashSet<Guid>>();

        extractor(entity, dict);

        Assert.Single(dict);
        Assert.Contains(id, dict[typeof(AggregateRoot)]);
    }

    [Fact]
    public void CompileIdExtractor_MultipleConfigs_AddsBoth()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var configs = new[]
        {
            new ReferenceConfiguration(
                typeof(AggregateRoot), typeof(RefCompilerTestEntity), typeof(AggregateRoot),
                nameof(RefCompilerTestEntity.OwnerId), ReferenceMode.Strict, []),
            new ReferenceConfiguration(
                typeof(AggregateRoot), typeof(RefCompilerTestEntity), typeof(EntityBase),
                nameof(RefCompilerTestEntity.NullableOwnerId), ReferenceMode.Strict, [])
        };
        var extractor = ReferenceIntegrityCompiler.CompileIdExtractor(typeof(RefCompilerTestEntity), configs);
        var entity = new RefCompilerTestEntity { OwnerId = id1, NullableOwnerId = id2 };
        var dict = new Dictionary<Type, HashSet<Guid>>();

        extractor(entity, dict);

        Assert.Equal(2, dict.Count);
        Assert.Contains(id1, dict[typeof(AggregateRoot)]);
        Assert.Contains(id2, dict[typeof(EntityBase)]);
    }
}