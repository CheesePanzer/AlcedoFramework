using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Framework.FormulaEngine.Contract;
using Alcedo.Framework.FormulaEngine.FormulaCenter;
using Alcedo.Framework.FormulaEngine.Infrastructure.Seeder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Alcedo.Framework.FormulaEngine;

public sealed class AlcedoFormulaEngineModule : AlcedoModuleBase
{
    protected override void AddServicesManually(IServiceCollection services, IConfiguration configuration, AlcedoModulesRegistry registry)
    {
        FormulaStoreCenter.OnInitialize(registry.Modules.Assemblies);

        services.AddScoped<IFormulaEngine, FormulaCenter.FormulaEngine>();
    }
}

public static class AlcedoFormulaEngineExtensions
{
    public static IHost SeedFormulaEngine(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        
        var context = scope.ServiceProvider.GetRequiredService<AlcedoDbContext>();

        FormulaSeeder.Seed(context);
        
        return host;
    }
}