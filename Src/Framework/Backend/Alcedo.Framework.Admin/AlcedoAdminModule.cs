using Alcedo.Core.Abstractions;
using Alcedo.Framework.Admin.Presentation.Middlemare;
using Microsoft.AspNetCore.Builder;

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

public static class AdminModuleExtensions
{
    public static IApplicationBuilder UseAlcedoAdminMiddlewares(this IApplicationBuilder app)
    {
        //app.UseMiddleware<AccessLogRecorderMiddleware>();
        return app;
    }
}