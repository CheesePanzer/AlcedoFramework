using Alcedo.Frontend.Core;
using Alcedo.Frontend.Identity.Providers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Frontend.Identity;

public class AlcedoIdentityModule : AlcedoModuleBase, IUseAlcedoModule
{
    public override void AddServicesToBuilder(WebAssemblyHostBuilder builder)
    {
        builder.Services.AddAuthorizationCore();
        builder.Services.AddSingleton<AuthStateProvider>();
        var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
                         ?? throw new InvalidOperationException("ApiBaseUrl is not configured");
        builder.Services.AddHttpClient("refresh", c => c.BaseAddress = new Uri(apiBaseUrl));
    }

    public void UseSelf(WebAssemblyHost host)
    {
        var authState = host.Services.GetRequiredService<AuthStateProvider>();
        var nav = host.Services.GetRequiredService<NavigationManager>();

        authState.OnUnauthenticated += () => { nav.NavigateTo("/redirect-to-login", replace: true); };
    }
}