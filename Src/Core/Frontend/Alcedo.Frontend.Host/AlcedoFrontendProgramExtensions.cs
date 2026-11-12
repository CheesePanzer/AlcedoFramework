using Alcedo.Frontend.Blazor;
using Alcedo.Frontend.Core;
using Alcedo.Frontend.Identity.Handlers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Frontend.Host;

public static class AlcedoFrontendProgramExtensions
{
    public static WebAssemblyHostBuilder AddAlcedoFrontend<TApp>(this WebAssemblyHostBuilder builder,
        AlcedoModulesRegistry registry,
        Action<AlcedoFrontendOptions>? configure=null)
    where TApp : ComponentBase
    {
        var options = new AlcedoFrontendOptions();
        configure?.Invoke(options);
        builder
            .AddAlcedoFrontEndCore(registry, options.Core)
            .AddAlcedoFrontEndBlazor<TApp>(registry, options.Blazor);
        return builder;
    }
}

public static class AlcedoUseModuleProgramExtensions
{
    public static WebAssemblyHost UseAlcedoModules(this WebAssemblyHost host)
    {
        var registry = host.Services.GetRequiredService<AlcedoModulesRegistry>();

        foreach (IAlcedoModule module in registry.Modules)
        {
            if (module is IUseAlcedoModule newModule)
            {
                newModule.UseSelf(host);
            }
        }
        
        return host;
    }
}