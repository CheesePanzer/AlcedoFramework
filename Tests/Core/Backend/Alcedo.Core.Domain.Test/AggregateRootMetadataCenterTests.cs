using System.Reflection;
using Alcedo.Core.Abstractions;
using Alcedo.Core.Domain.Attributes;
using Alcedo.Core.Domain.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Domain.Test;

[AggregateRootDescription("META", "MetadataAgg")]
public class MetadataTestAgg : AggregateRoot;

[Collection("AggregateRootMetadataCenter")]
public class AggregateRootMetadataCenterTests
{
    private static TestMetadataModule CreateModule() => new(typeof(MetadataTestAgg).Assembly);

    private static void ResetCenter() => AggregateRootMetadataCenter.Initialize(Array.Empty<IAlcedoModule>());

    [Fact]
    public void Initialize_PopulatesMetadataCorrectly()
    {
        AggregateRootMetadataCenter.Initialize([CreateModule()]);

        var meta = AggregateRootMetadataCenter.GetMetaData<MetadataTestAgg>();

        Assert.Equal("META", meta.Code);
        Assert.Equal("MetadataAgg", meta.Name);
        Assert.Equal("test", meta.ModuleCode);
        Assert.Equal("test_module", meta.ModuleName);
    }

    [Fact]
    public void GetMetaData_ComputesFullNameAndIdentity()
    {
        AggregateRootMetadataCenter.Initialize([CreateModule()]);

        var meta = AggregateRootMetadataCenter.GetMetaData<MetadataTestAgg>();

        Assert.Equal("test_module.MetadataAgg", meta.FullName);
        Assert.Equal("TEST_META", meta.Identity);
    }

    [Fact]
    public void GetMetaData_ByType_ReturnsCorrectMetadata()
    {
        AggregateRootMetadataCenter.Initialize([CreateModule()]);

        var meta = AggregateRootMetadataCenter.GetMetaData(typeof(MetadataTestAgg));

        Assert.Equal("META", meta.Code);
    }

    [Fact]
    public void GetAggregateTypeByIdentity_ReturnsCorrectType()
    {
        var type = AggregateRootMetadataCenter.GetAggregateTypeByIdentity("TEST_META");

        Assert.Equal(typeof(MetadataTestAgg), type);
    }

    [Fact]
    public void GetAggregateTypeByIdentity_IsCaseInsensitive()
    {
        AggregateRootMetadataCenter.Initialize([CreateModule()]);

        var type = AggregateRootMetadataCenter.GetAggregateTypeByIdentity("test_meta");

        Assert.Equal(typeof(MetadataTestAgg), type);
    }

    [Fact]
    public void AggregateRootExtensions_GetMetaData_ReturnsCorrectMetadata()
    {
        AggregateRootMetadataCenter.Initialize([CreateModule()]);

        var agg = new MetadataTestAgg();

        var meta = agg.GetMetaData();


        Assert.Equal("META", meta.Code);
    }

    [Fact]
    public void Initialize_ThrowsWhenDuplicateIdentity()
    {
        // Two modules pointing to the same assembly will scan the same types twice,
        // causing duplicate identity entries.
        var module = CreateModule();
        Assert.Throws<InvalidOperationException>(() =>
            AggregateRootMetadataCenter.Initialize([module, module]));

        ResetCenter();
    }

    [Fact]
    public void GetMetaData_ThrowsForNonAggregateRootType()
    {
        AggregateRootMetadataCenter.Initialize([CreateModule()]);

        Assert.Throws<InvalidOperationException>(() =>
            AggregateRootMetadataCenter.GetMetaData(typeof(string)));
    }

    [Fact]
    public void GetMetaData_ThrowsWhenNotRegistered()
    {
        ResetCenter();

        Assert.Throws<InvalidOperationException>(() =>
            AggregateRootMetadataCenter.GetMetaData<MetadataTestAgg>());
    }

    [Fact]
    public void GetAggregateTypeByIdentity_ThrowsWhenNotFound()
    {
        ResetCenter();

        Assert.Throws<InvalidOperationException>(() =>
            AggregateRootMetadataCenter.GetAggregateTypeByIdentity("NOT_FOUND"));
    }
}

internal class TestMetadataModule : IAlcedoModule
{
    private readonly Assembly _assembly;

    public TestMetadataModule(Assembly assembly)
    {
        _assembly = assembly;
    }

    public string ModuleCode => "test";
    public string ModuleName => "test_module";
    public string ModuleVersion => "1.0.0";
    public Assembly Assembly => _assembly;

    public void AddServicesToBuilder(IServiceCollection services, IConfiguration configuration,
        AlcedoModulesRegistry registry)
    {
    }
}