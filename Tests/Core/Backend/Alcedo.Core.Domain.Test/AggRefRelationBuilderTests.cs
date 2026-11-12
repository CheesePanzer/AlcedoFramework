using System.Reflection;
using Alcedo.Core.Domain.Attributes;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Domain.Test;

[AggregateRootDescription("ORD", "Order")]
public class TestOrderAgg : AggregateRoot
{
    public Guid? ProductId { get; set; }
    public List<TestOrderItem> Items { get; set; } = [];
    public TestOrderItem? SingleItem { get; set; }
}

public class TestOrderItem : EntityBase
{
    public Guid? ProductId { get; set; }
    public TestDetail? Detail { get; set; }
}

public class TestDetail : EntityBase
{
    public Guid? SupplierId { get; set; }
}

[AggregateRootDescription("PRD", "Product")]
public class TestProductAgg : AggregateRoot;

[AggregateRootDescription("SUP", "Supplier")]
public class TestSupplierAgg : AggregateRoot;

[Collection("AggregateRootMetadataCenter")]
public class AggRefRelationBuilderTests
{
    [Fact]
    public void Entity_ReferencesDirectly_CreatesConfigWithEmptyNavigationPath()
    {
        var builder = new ReferenceModelBuilder();

        var entityBuilder = builder.Entity<TestOrderAgg>();
        entityBuilder.References<TestProductAgg>(o => o.ProductId);

        var configs = builder.GetConfigs();
        Assert.Single(configs);

        var config = configs[0];
        Assert.Equal(typeof(TestOrderAgg), config.SourceAggregateType);
        Assert.Equal(typeof(TestOrderAgg), config.DependentEntityType);
        Assert.Equal(typeof(TestProductAgg), config.PrincipalAggregateType);
        Assert.Equal("ProductId", config.ForeignKeyProperty);
        Assert.Equal(ReferenceMode.Strict, config.ReferenceMode);
        Assert.Empty(config.NavigationPath);
    }

    [Fact]
    public void Entity_ThroughThenReferences_CreatesConfigWithSingleNavigationStep()
    {
        var builder = new ReferenceModelBuilder();

        builder.Entity<TestOrderAgg>()
            .Through(o => o.Items)
            .References<TestProductAgg>(i => i.ProductId);

        var configs = builder.GetConfigs();
        Assert.Single(configs);

        var config = configs[0];
        Assert.Equal(typeof(TestOrderAgg), config.SourceAggregateType);
        Assert.Equal(typeof(TestOrderItem), config.DependentEntityType);
        Assert.Equal(typeof(TestProductAgg), config.PrincipalAggregateType);
        Assert.Equal("ProductId", config.ForeignKeyProperty);
        Assert.Single(config.NavigationPath);

        var step = config.NavigationPath[0];
        Assert.Equal(typeof(TestOrderAgg), step.ParentEntityType);
        Assert.Equal("Items", step.PropertyName);
        Assert.Equal(typeof(TestOrderItem), step.PropertyType);
        Assert.True(step.IsCollection);
    }

    [Fact]
    public void Entity_MultiLevelThrough_CreatesConfigWithMultipleNavigationSteps()
    {
        var builder = new ReferenceModelBuilder();

        builder.Entity<TestOrderAgg>()
            .Through(o => o.Items)
            .Through(i => i.Detail!)
            .References<TestSupplierAgg>(d => d.SupplierId);

        var configs = builder.GetConfigs();
        Assert.Single(configs);

        var config = configs[0];
        Assert.Equal(2, config.NavigationPath.Count);

        var firstStep = config.NavigationPath[0];
        Assert.Equal(typeof(TestOrderAgg), firstStep.ParentEntityType);
        Assert.Equal("Items", firstStep.PropertyName);
        Assert.True(firstStep.IsCollection);

        var secondStep = config.NavigationPath[1];
        Assert.Equal(typeof(TestOrderItem), secondStep.ParentEntityType);
        Assert.Equal("Detail", secondStep.PropertyName);
        Assert.False(secondStep.IsCollection);
        Assert.Equal(typeof(TestDetail), secondStep.PropertyType);
    }

    [Fact]
    public void Entity_MultipleReferencesOnSameEntity_CreatesMultipleConfigs()
    {
        var builder = new ReferenceModelBuilder();

        builder.Entity<TestOrderAgg>()
            .Through(o => o.Items)
            .References<TestProductAgg>(i => i.ProductId);

        builder.Entity<TestOrderAgg>()
            .Through(o => o.Items)
            .Through(i => i.Detail!)
            .References<TestSupplierAgg>(d => d.SupplierId);

        var configs = builder.GetConfigs();
        Assert.Equal(2, configs.Count);
    }

    [Fact]
    public void References_WithSnapshotMode_SetsModeCorrectly()
    {
        var builder = new ReferenceModelBuilder();

        builder.Entity<TestOrderAgg>()
            .References<TestProductAgg>(o => o.ProductId, ReferenceMode.Snapshot);

        var config = builder.GetConfigs()[0];
        Assert.Equal(ReferenceMode.Snapshot, config.ReferenceMode);
    }

    [Fact]
    public void Entity_ThroughNonCollectionThenReferences_CreatesConfigWithNavigationStep()
    {
        var builder = new ReferenceModelBuilder();

        builder.Entity<TestOrderAgg>()
            .Through(o => o.SingleItem!)
            .References<TestProductAgg>(i => i.ProductId);

        var configs = builder.GetConfigs();
        Assert.Single(configs);

        var config = configs[0];
        Assert.Equal(typeof(TestOrderAgg), config.SourceAggregateType);
        Assert.Equal(typeof(TestOrderItem), config.DependentEntityType);
        Assert.Single(config.NavigationPath);

        var step = config.NavigationPath[0];
        Assert.Equal(typeof(TestOrderAgg), step.ParentEntityType);
        Assert.Equal("SingleItem", step.PropertyName);
        Assert.Equal(typeof(TestOrderItem), step.PropertyType);
        Assert.False(step.IsCollection);
    }

    [Fact]
    public void Entity_ReferencesByPrincipalCode_CreatesConfigWithResolvedType()
    {
        AggregateRootMetadataCenter.Initialize([
            new TestAggRefModule(typeof(TestProductAgg).Assembly)
        ]);

        var builder = new ReferenceModelBuilder();

        builder.Entity<TestOrderAgg>()
            .References("TEST_PRD", o => o.ProductId);

        var configs = builder.GetConfigs();
        Assert.Single(configs);

        var config = configs[0];
        Assert.Equal(typeof(TestProductAgg), config.PrincipalAggregateType);
        Assert.Equal("ProductId", config.ForeignKeyProperty);
    }

    [Fact]
    public void Entity_ReferencesByPrincipalCode_InvalidCode_Throws()
    {
        AggregateRootMetadataCenter.Initialize([
            new TestAggRefModule(typeof(TestProductAgg).Assembly)
        ]);

        var builder = new ReferenceModelBuilder();

        Assert.Throws<InvalidOperationException>(() =>
            builder.Entity<TestOrderAgg>().References("INVALID", o => o.ProductId));
    }
}

internal class TestAggRefModule : Alcedo.Core.Abstractions.IAlcedoModule
{
    private readonly Assembly _assembly;

    public TestAggRefModule(Assembly assembly)
    {
        _assembly = assembly;
    }

    public string ModuleCode => "test";
    public string ModuleName => "test_module";
    public string ModuleVersion => "1.0.0";
    public Assembly Assembly => _assembly;

    public void AddServicesToBuilder(IServiceCollection services, IConfiguration configuration,
        Alcedo.Core.Abstractions.AlcedoModulesRegistry registry)
    {
    }
}