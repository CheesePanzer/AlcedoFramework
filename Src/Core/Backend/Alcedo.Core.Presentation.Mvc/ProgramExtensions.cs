using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Presentation.Mvc;

public static class ProgramExtensions
{
    internal static IServiceCollection AddAlcedoCorePresentationMvcSupport(this IServiceCollection services)
    {
        services.AddExceptionHandler<AlcedoMvcExceptionHandler>();
        
        var controllerBuilder = services.AddControllersWithViews();
        
        controllerBuilder.AddApplicationPart(AlcedoCorePresentationMvcComponent.Assembly);
        
        return services;
    }

    internal static IApplicationBuilder UseAlcedoCorePresentationMvcSupport(this IApplicationBuilder app)
    {
        app.UseStaticFiles();
        app.UseStatusCodePagesWithReExecute("/Error/Status");

        return app;
    }
}