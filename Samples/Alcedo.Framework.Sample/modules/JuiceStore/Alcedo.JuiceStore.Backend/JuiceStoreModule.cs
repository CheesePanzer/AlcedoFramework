using Alcedo.Core.Abstractions;

namespace Alcedo.JuiceStore.Backend;

public sealed class JuiceStoreModule() : AlcedoModuleBase("juicestore", "Juice Store");

public static class JuiceStoreModuleRegistryExtensions
{
    public static AlcedoModulesRegistry AddJuiceStore(this AlcedoModulesRegistry registry)
    {
        registry.AddModule(new JuiceStoreModule());
        return registry;
    }
}
