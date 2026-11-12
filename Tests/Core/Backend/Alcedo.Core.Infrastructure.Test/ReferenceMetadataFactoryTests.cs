using Alcedo.Core.Domain.Attributes;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Core.Infrastructure.ReferenceIntegrityImpl;

namespace Alcedo.Core.Infrastructure.Test;

[AggregateRootDescription("METASUP")]
public class MetaFactorySupplierAgg : AggregateRoot
{
    public MetaFactorySupplierAgg()
    {
    }
}

[AggregateRootDescription("METAPRD")]
public class MetaFactoryProductAgg : AggregateRoot
{
    public Guid? SupplierId { get; set; }
    public List<MetaFactoryVariant> Variants { get; set; } = new();

    public MetaFactoryProductAgg()
    {
    }
}

public class MetaFactoryVariant : EntityBase
{
    public Guid? SupplierId { get; set; }
}

public class MetaFactoryProductConfig : IAggregateRootReferenceConfiguration<MetaFactoryProductAgg>
{
    public void Configure(ReferenceEntityBuilder<MetaFactoryProductAgg> builder)
    {
        builder.References<MetaFactorySupplierAgg>(x => x.SupplierId);
        builder.Through(x => x.Variants).References<MetaFactorySupplierAgg>(x => x.SupplierId);
    }
}

public class ReferenceMetadataFactoryTests
{
    [Fact]
    public void Create_ScansConfigs_ReturnsStore()
    {
        ReferenceConfigurationCenter.Initialize([typeof(ReferenceMetadataFactoryTests).Assembly]);
        var store = ReferenceMetadataFactory.Create();

        Assert.NotNull(store);
        Assert.NotNull(store.CheckFuncs);
        Assert.NotNull(store.AggregateCollectors);
    }

    [Fact]
    public void Create_CheckFuncs_ContainsPrincipalKey()
    {
        ReferenceConfigurationCenter.Initialize([typeof(ReferenceMetadataFactoryTests).Assembly]);
        var store = ReferenceMetadataFactory.Create();

        Assert.True(store.CheckFuncs.ContainsKey(typeof(MetaFactorySupplierAgg)));
    }

    [Fact]
    public void Create_CheckFuncs_GroupsBySourceAggregate()
    {
        ReferenceConfigurationCenter.Initialize([typeof(ReferenceMetadataFactoryTests).Assembly]);
        var store = ReferenceMetadataFactory.Create();

        var supplierValidators = store.CheckFuncs[typeof(MetaFactorySupplierAgg)];
        Assert.True(supplierValidators.ContainsKey(typeof(MetaFactoryProductAgg)));
    }

    [Fact]
    public void Create_AggregateCollectors_ContainsSourceAggregate()
    {
        ReferenceConfigurationCenter.Initialize([typeof(ReferenceMetadataFactoryTests).Assembly]);
        var store = ReferenceMetadataFactory.Create();

        Assert.True(store.AggregateCollectors.ContainsKey(typeof(MetaFactoryProductAgg)));
    }

    [Fact]
    public void Create_AggregateCollector_ExtractsDirectReferences()
    {
        ReferenceConfigurationCenter.Initialize([typeof(ReferenceMetadataFactoryTests).Assembly]);
        var store = ReferenceMetadataFactory.Create();
        var collector = store.AggregateCollectors[typeof(MetaFactoryProductAgg)];

        var id = Guid.NewGuid();
        var agg = new MetaFactoryProductAgg { SupplierId = id };
        var result = collector(agg);

        Assert.True(result.ContainsKey(typeof(MetaFactorySupplierAgg)));
        Assert.Contains(id, result[typeof(MetaFactorySupplierAgg)]);
    }

    [Fact]
    public void Create_AggregateCollector_ExtractsNavigatedReferences()
    {
        ReferenceConfigurationCenter.Initialize([typeof(ReferenceMetadataFactoryTests).Assembly]);
        var store = ReferenceMetadataFactory.Create();
        var collector = store.AggregateCollectors[typeof(MetaFactoryProductAgg)];

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var agg = new MetaFactoryProductAgg
        {
            SupplierId = id1,
            Variants =
            [
                new MetaFactoryVariant { SupplierId = id2 }
            ]
        };
        var result = collector(agg);

        Assert.True(result.ContainsKey(typeof(MetaFactorySupplierAgg)));
        Assert.Contains(id1, result[typeof(MetaFactorySupplierAgg)]);
        Assert.Contains(id2, result[typeof(MetaFactorySupplierAgg)]);
    }

    [Fact]
    public void Create_AggregateCollector_SkipsEmptyGuids()
    {
        ReferenceConfigurationCenter.Initialize([typeof(ReferenceMetadataFactoryTests).Assembly]);
        var store = ReferenceMetadataFactory.Create();
        var collector = store.AggregateCollectors[typeof(MetaFactoryProductAgg)];

        var agg = new MetaFactoryProductAgg
        {
            SupplierId = Guid.Empty,
            Variants = [new MetaFactoryVariant { SupplierId = null }]
        };
        var result = collector(agg);

        Assert.False(result.ContainsKey(typeof(MetaFactorySupplierAgg)));
    }
}