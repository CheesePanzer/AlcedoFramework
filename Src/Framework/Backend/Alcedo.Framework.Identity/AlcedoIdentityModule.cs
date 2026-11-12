using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Framework.Identity.Application.Service.ContractImpl;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Infrastructure.Seeder;
using Alcedo.Framework.Identity.Presentation;
using Alcedo.Framework.Identity.ProgramExtensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Alcedo.Framework.Identity;

public static class AlcedoModuleRegistryExtensions
{
    public static AlcedoModulesRegistry AddAlcedoIdentity(this AlcedoModulesRegistry registry)
    {
        registry.AddModule(new AlcedoIdentityModule());
        return registry;
    }
}

public class AlcedoIdentityModule : AlcedoModuleBase
{
    protected override void AddServicesManually(IServiceCollection services, IConfiguration configuration,
        AlcedoModulesRegistry registry)
    {
        services.AddScoped<IOperatorModuleService, OperatorModuleService>();

        services.AddAlcedoIdentityAuthentication(configuration);

        services.AddAlcedoIdentityAuthorization();
    }
}

public static class FrameworkIdentityModuleExtensions
{
    private static void SeedRootUser(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AlcedoDbContext>();
        RootUserSeeder.Seed(context);
    }

    public static void SeedAlcedoIdentity(this IHost host)
    {
        SeedRootUser(host);
    }

    public static IApplicationBuilder UseAlcedoIdentityMiddlewares(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseMiddleware<AuthSessionMiddleware>();
        app.UseAuthorization();
        return app;
    }
}