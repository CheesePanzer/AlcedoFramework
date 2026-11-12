using Alcedo.Frontend.Admin;
using Alcedo.Frontend.Blazor;
using Alcedo.Frontend.Core;
using Alcedo.Frontend.FormulaEngine;
using Alcedo.Frontend.Host;
using Alcedo.Frontend.Identity;
using Alcedo.Frontend.Identity.Handlers;
using Alcedo.Frontend.Notification;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Alcedo.Frontend.Sample;
using Alcedo.JuiceStore.Frontend;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

AlcedoModulesRegistry registry = 
    new AlcedoModulesRegistry()
        .AddModules(
            new AlcedoIdentityModule(),
            new AlcedoAdminModule(), 
            new AlcedoNotificationModule(), 
            new AlcedoFormulaEngineModule(),
            new JuiceStoreFrontendModule());

builder.AddAlcedoFrontend<App>(registry, opt =>
{
    opt.Core.AdditionalClientHandlers = [typeof(TokenRefreshHandler)];
});

var host = builder.Build();

host.UseAlcedoModules();

await host.RunAsync();
