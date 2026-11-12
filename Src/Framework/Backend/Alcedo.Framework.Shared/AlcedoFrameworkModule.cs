using Alcedo.Core.Abstractions;
using Alcedo.Framework.Shared.Application;
using Alcedo.Framework.Shared.Application.Components.MasterData;
using Alcedo.Framework.Shared.Application.Components.ReferenceData;
using Alcedo.Framework.Shared.Application.IQueryRepository;
using Alcedo.Framework.Shared.Application.Service;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using Alcedo.Framework.Shared.Infrastructure.QueryRepository;
using Alcedo.Framework.Shared.Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Framework.Shared;

public class AlcedoFrameworkModule : AlcedoModuleBase
{
    protected override void AddServicesManually(IServiceCollection services, IConfiguration configuration,
        AlcedoModulesRegistry registry)
    {
        services.AddScoped<IMasterDataStatusValidator, MasterDataStatusValidator>();
        RegisterTemplateGenericService(services);
    }

    private void RegisterTemplateGenericService(IServiceCollection services)
    {
        services.AddScoped(
            typeof(IMasterDataAuditLogQueryRepository<>),
            typeof(MasterDataAuditLogQueryRepository<>));

        services.AddScoped(typeof(MasterDataLogSearchService<>));

        services.AddScoped(
            typeof(IReferenceDataRepositoryBase<>),
            typeof(ReferenceDataRepositoryBase<>));

        services.AddScoped(
            typeof(IMasterDataRepositoryBase<>),
            typeof(MasterDataRepositoryBase<>));
        
        services.AddScoped(
            typeof(IMasterDataQueryRepositoryBase<,,>),
            typeof(MasterDataQueryRepositoryBaseBase<,,>));

        services.AddScoped(typeof(ReferenceDataSearchServiceComponent<,,>));

        services.AddScoped(typeof(MasterDataSearchServiceComponent<,,>));

        services.AddScoped(typeof(MasterDataStatusServiceComponent<>));
    }
}

public static class AlcedoModuleRegistryExtensions
{
    public static AlcedoModulesRegistry AddAlcedoFrameworkBase(this AlcedoModulesRegistry registry)
    {
        registry.AddModule(new AlcedoFrameworkModule());
        return registry;
    }
}