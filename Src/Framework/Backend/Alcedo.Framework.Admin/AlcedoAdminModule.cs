using Alcedo.Core.Abstractions;

namespace Alcedo.Framework.Admin;

public sealed class AlcedoAdminModule : AlcedoModuleBase;

public static class AlcedoModuleRegistryExtensions
{
    public static AlcedoModulesRegistry AddAlcedoAdmin(this AlcedoModulesRegistry registry)
    {
        registry.AddModule(new AlcedoAdminModule());
        return registry;
    }
}
