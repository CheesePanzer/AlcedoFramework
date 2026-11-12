using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Core.Infrastructure.ReferenceIntegrityImpl;

namespace Alcedo.Core.Infrastructure.Test;

public class ReferenceIntegrityConfigCenterTests
{
    private static ReferenceIntegrityConfigCenter CreateCenter()
        => new();

    [Fact]
    public void GetRefCheckFuncs_Generic_ReturnsFuncs()
    {
        ReferenceConfigurationCenter.Initialize([typeof(ReferenceIntegrityConfigCenterTests).Assembly]);
        var center = CreateCenter();

        var funcs = center.GetRefCheckFuncs<MetaFactoryProductAgg>();

        Assert.NotNull(funcs);
    }

    [Fact]
    public void GetRefCheckFuncs_Type_ReturnsFuncs()
    {
        ReferenceConfigurationCenter.Initialize([typeof(ReferenceIntegrityConfigCenterTests).Assembly]);
        var center = CreateCenter();

        var funcs = center.GetRefCheckFuncs(typeof(MetaFactoryProductAgg));

        Assert.NotNull(funcs);
    }

    [Fact]
    public void GetRefCheckFuncs_NonAggregateRoot_ThrowsArgumentException()
    {
        ReferenceConfigurationCenter.Initialize([typeof(ReferenceIntegrityConfigCenterTests).Assembly]);
        var center = CreateCenter();

        Assert.Throws<ArgumentException>(() => center.GetRefCheckFuncs(typeof(EntityBase)));
    }

    [Fact]
    public void GetRefCheckFuncs_MissingType_ReturnsEmpty()
    {
        ReferenceConfigurationCenter.Initialize([typeof(ReferenceIntegrityConfigCenterTests).Assembly]);
        var center = new ReferenceIntegrityConfigCenter();

        var funcs = center.GetRefCheckFuncs(typeof(MetaFactoryProductAgg));

        Assert.Empty(funcs);
    }

    [Fact]
    public void GetRefIdCollectorFunc_Generic_ReturnsFunc()
    {
        ReferenceConfigurationCenter.Initialize([typeof(ReferenceIntegrityConfigCenterTests).Assembly]);
        var center = CreateCenter();

        var func = center.GetRefIdCollectorFunc<MetaFactoryProductAgg>();

        Assert.NotNull(func);
    }

    [Fact]
    public void GetRefIdCollectorFunc_Type_ReturnsFunc()
    {
        ReferenceConfigurationCenter.Initialize([typeof(ReferenceIntegrityConfigCenterTests).Assembly]);
        var center = CreateCenter();

        var func = center.GetRefIdCollectorFunc(typeof(MetaFactoryProductAgg));

        Assert.NotNull(func);
    }

    [Fact]
    public void GetRefIdCollectorFunc_NonAggregateRoot_ThrowsArgumentException()
    {
        ReferenceConfigurationCenter.Initialize([typeof(ReferenceIntegrityConfigCenterTests).Assembly]);
        var center = CreateCenter();

        Assert.Throws<ArgumentException>(() => center.GetRefIdCollectorFunc(typeof(EntityBase)));
    }

    [Fact]
    public void GetRefIdCollectorFunc_MissingType_ReturnsNull()
    {
        ReferenceConfigurationCenter.Initialize([]);
        var center = new ReferenceIntegrityConfigCenter();

        var func = center.GetRefIdCollectorFunc(typeof(MetaFactoryProductAgg));

        Assert.Null(func);
    }
}