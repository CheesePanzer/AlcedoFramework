using Alcedo.Core.Application.Component;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Application;

internal static class ProgramExtensions
{
    public static void AddAlcedoCoreApplication(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;

        config.ForType<StateValue, string>().MapWith(src => src.Value);

        services.AddSingleton(config);

        services.AddScoped<IMapper, ServiceMapper>();

        services.AddScoped<ApplicationLockProvider>();

        services.AddScoped(typeof(EntitySearchServiceComponent<,,>));

        services.AddScoped(typeof(AggregateRootSearchServiceComponent<,,>));
    }
}