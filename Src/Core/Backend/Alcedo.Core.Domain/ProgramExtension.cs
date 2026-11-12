using Alcedo.Core.Abstractions;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Domain;

internal static class ProgramExtension
{
    public static IServiceCollection AddAlcedoCoreDomain(this IServiceCollection service,
        AlcedoModulesRegistry registry)
    {
        AggregateRootMetadataCenter.Initialize(registry.Modules);
        
        ReferenceConfigurationCenter.Initialize(registry.Modules.Assemblies);

        return service;
    }
}