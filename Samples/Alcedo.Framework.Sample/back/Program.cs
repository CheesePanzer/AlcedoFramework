using Alcedo.Core.Abstractions;
using Alcedo.Core.Host;
using Alcedo.Framework.Admin;
using Alcedo.Framework.FormulaEngine;
using Alcedo.Framework.Identity;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Notification;
using Alcedo.Framework.Shared;
using Alcedo.JuiceStore.Backend;

Serilog.Debugging.SelfLog.Enable(Console.Error);

AlcedoModulesRegistry registry = new AlcedoModulesRegistry()
    .AddAlcedoFrameworkBase()
    .AddAlcedoAdmin()
    .AddAlcedoIdentity()
    .AddAlcedoNotification()
    .AddModule(new AlcedoFormulaEngineModule())
    .AddJuiceStore();

var builder = WebApplication.CreateBuilder(args);

builder.CreateAlcedoBuilder(registry)
    .AddCore(options =>
    {
        options.Infrastructure.MigrationAssembly = typeof(Program).Assembly;
        options.Infrastructure.UsePostgresMqTransfer = true;
        options.Presentation.DisplaySensitiveExceptions = true;
        options.Presentation.AddMvcSupport = true;
    })
    .AddModules()
    .Build();

var app = builder.Build();

app.UseAlcedoCoreOnStartUp();

app.UseAlcedoCoreMiddlewares()
    .UseAlcedoAdminMiddlewares()
    .UseAlcedoIdentityMiddlewares();

app.MapAlcedoCoreControllers();

app.SeedAlcedoIdentity();
app.SeedFormulaEngine();

app.Run();
