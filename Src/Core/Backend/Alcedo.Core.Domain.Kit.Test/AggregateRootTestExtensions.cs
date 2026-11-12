using System.Reflection;
using Alcedo.Core.Abstractions;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.Event;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Domain.Kit.Test;

public static class AggregateRootTestExtensions
{
    public static ICollection<DomainEventBase> CollectEventsAndClearForTest(this AggregateRoot root)
        => root.CollectEventsAndClear();

    public static void ClearEventsForTest(this AggregateRoot root)
        => root.ClearEvents();

    public static void TouchForCreateForTest(this AggregateRoot root)
        => root.TouchForCreate();

    public static void TouchForUpdateForTest(this AggregateRoot root)
        => root.TouchForUpdate();

    public static void ResetModifiedForTest(this AggregateRoot root)
        => root.ResetModified();
}

public static class MetadataCenterTestExtensions
{
    public static void InitializeForTest(Assembly assembly)
        => AggregateRootMetadataCenter.Initialize([new TestModule(assembly)]);

    public static void ResetForTest()
        => AggregateRootMetadataCenter.Initialize(Array.Empty<IAlcedoModule>());

    private class TestModule : IAlcedoModule
    {
        private readonly Assembly _assembly;
        public TestModule(Assembly assembly) => _assembly = assembly;
        public string ModuleCode => "test";
        public string ModuleName => "test";
        public string ModuleVersion => "1.0.0";
        public Assembly Assembly => _assembly;
        public void AddServicesToBuilder(IServiceCollection services, IConfiguration configuration,
            AlcedoModulesRegistry registry) { }
    }
}
