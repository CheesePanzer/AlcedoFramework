using System.Reflection;
using Alcedo.Frontend.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace Alcedo.Frontend.Blazor;

public sealed class AlcedoFrontendBlazorOptions
{
    public string AppDisplayName { get; set; } = "Alcedo Atthis";
}

internal static class ProgramExtensions
{
    public static WebAssemblyHostBuilder AddAlcedoFrontEndBlazor<TApp>(this WebAssemblyHostBuilder builder,
        AlcedoModulesRegistry registry, AlcedoFrontendBlazorOptions options)
    where TApp : ComponentBase
    {
        builder.RootComponents.Add<TApp>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<ThemeStateProvider>();
        builder.Services.AddSingleton<GlobalOverlayService>();
        builder.Services.AddMudServices();
        builder.Services.AddScoped<AutoValidatorService>();
        builder.Services.AddScoped<AlcedoRequestService>();
        builder.Services.AddNavItem(registry.Modules.Assemblies);
        builder.Services.AddBarItem(registry.Modules.Assemblies);
        return builder;
    }

    public static IServiceCollection AddNavItem(this IServiceCollection services,
        IEnumerable<Assembly> assemblies)
    {
        services.AddSingleton<NavRegistry>(sp =>
        {
            var registry = new NavRegistry();

            var contributorTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(INavContributor)));

            foreach (var type in contributorTypes)
            {
                var contributor = (INavContributor)Activator.CreateInstance(type)!;
                foreach (var item in contributor.GetItems())
                    registry.Register(item);
            }

            return registry;
        });
        
        return services;
    }

    public static IServiceCollection AddBarItem(this IServiceCollection service,
        IEnumerable<Assembly> assemblies)
    {
        service.AddSingleton<BarRegistry>(sp =>
        {
            var registry = new BarRegistry();

            var groupTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(IBarItem)));

            foreach (var type in groupTypes)
            {
                var group = (IBarItem)Activator.CreateInstance(type)!;
                registry.Register(group);
            }

            return registry;
        });

        return service;
    }
}