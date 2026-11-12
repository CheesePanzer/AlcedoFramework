using System.Reflection;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Infrastructure.ReferenceIntegrityImpl;

public static class ReferenceIntegrityRegistry
{
    public static IServiceCollection AddReferenceIntegrityTool(
        this IServiceCollection services)
    {
        services.AddSingleton<ReferenceIntegrityConfigCenter>();

        services.AddScoped<IAggRefChecker, AggRefChecker>();

        services.AddScoped<IAggRefScanner, AggRefScanner>();

        return services;
    }

    public static IApplicationBuilder UseReferenceIntegrityTool(this IApplicationBuilder app)
    {
        _ = app.ApplicationServices.GetRequiredService<ReferenceIntegrityConfigCenter>();

        return app;
    }
}