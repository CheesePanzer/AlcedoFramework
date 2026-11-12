using System.Reflection;
using Alcedo.Core.Abstractions;
using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.IRepositoryBase;
using Alcedo.Core.Domain.IStorageService;
using Alcedo.Core.Infrastructure.Job;
using Alcedo.Core.Infrastructure.ProgramExtensions;
using Alcedo.Core.Infrastructure.QueryRepositoryBase;
using Alcedo.Core.Infrastructure.ReferenceIntegrityImpl;
using Alcedo.Core.Infrastructure.RepositoryBase;
using Alcedo.Core.Infrastructure.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Alcedo.Core.Infrastructure;

public sealed record AlcedoInfrastructureOptions
{
    public bool WarmUpDbContextOnStartup { get; set; } = true;
    
    public Assembly? MigrationAssembly { get; set; }

    public string? MasstransitScheme { get; set; }

    public string? QuartzScheme { get; set; }
    
    public bool UsePostgresMqTransfer { get; set; }
}

internal static class ProgramServiceRegisterExtensions
{
    public static void AddAlcedoCoreInfrastructure(this IServiceCollection services, ConfigureHostBuilder host,
        IConfiguration configuration, AlcedoModulesRegistry registry,
        IWebHostEnvironment environment,
        AlcedoInfrastructureOptions options)
    {
        host.UseSerilog((ctx, config) => config
            .ReadFrom.Configuration(ctx.Configuration)
            .Enrich.FromLogContext());

        services.BindingAppSettingsToOptions(configuration, registry.Modules.Assemblies);

        services.AddAlcedoCoreDbContext(configuration, registry.Modules,
            environment, options);

        services.AddAlcedoRedis(configuration, registry.Modules.Assemblies);

        services.AddAlcedoMassTransitPublic(configuration, registry.Modules.Assemblies, options);

        services.AddAlcedoCoreStorage(configuration);

        services.AddAutoQuartzJobs(configuration, registry.Modules.Assemblies);

        services.AddScoped<TransactionManager>();

        services.AddScoped<AggregateEventsCollector>();

        services.AddScoped<IDistributedLockFactory, DistributedLockFactory>();

        services.AddScoped<IUnitOfWorkExecutor, UnitOfWorkExecutor>();

        services.AddScoped<IIdempotencyKeyProvider, IdempotencyKeyProvider>();
        
        services.AddSingleton<IStorageKeyCache, StorageKeyCache>();
        
        services.AddScoped<IStorageGcExecutor, StorageGcExecutor>();

        services.AddReferenceIntegrityTool();

        services.RegisterGenericTemplateService();
    }

    private static void RegisterGenericTemplateService(this IServiceCollection services)
    {
        services.AddScoped(typeof(IEntityStoreBase<>), typeof(EntityStoreBase<>));

        services.AddScoped(typeof(IAggregateRootRepositoryBase<>), typeof(AggregateRootRepositoryBase<>));

        services.AddScoped(typeof(IEntityQueryRepository<,,>), typeof(EntityQueryRepositoryBase<,,>));
    }
}