using System.Net;
using System.Reflection;
using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Domain;
using Alcedo.Core.Infrastructure;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Core.Infrastructure.ProgramExtensions;
using Alcedo.Core.Infrastructure.ReferenceIntegrityImpl;
using Alcedo.Core.Plugin.SmartDoc;
using Alcedo.Core.Presentation;
using Alcedo.Core.Presentation.Middlewares;
using Alcedo.Core.Presentation.Mvc;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using IPNetwork = System.Net.IPNetwork;

namespace Alcedo.Core.Host;

public sealed class AlcedoBuilder(
    WebApplicationBuilder webBuilder,
    AlcedoModulesRegistry registry,
    Assembly entryPointAssembly)
{
    public Assembly EntryPointAssembly => entryPointAssembly;

    public WebApplicationBuilder WebBuilder { get; } = webBuilder;

    public AlcedoModulesRegistry Registry { get; } = registry;

    public IWebHostEnvironment WebHostEnvironment => WebBuilder.Environment;

    public IConfiguration Configuration => WebBuilder.Configuration;

    public IServiceCollection Services => WebBuilder.Services;

    public ConfigureHostBuilder Host => WebBuilder.Host;

    public WebApplicationBuilder Build() => WebBuilder;
}

public static class WebApplicationBuilderExtensions
{
    public static AlcedoBuilder CreateAlcedoBuilder(this WebApplicationBuilder builder,
        AlcedoModulesRegistry registry)
    {
        builder.Services.AddSingleton(registry);
        return new AlcedoBuilder(builder, registry, Assembly.GetEntryAssembly()!);
    }
}

public static class AlcedoCoreBuilderExtension
{
    public static AlcedoBuilder AddCore(this AlcedoBuilder builder, Action<AlcedoCoreOptions>? configure = null)
    {
        var options = new AlcedoCoreOptions();
        configure?.Invoke(options);
        builder.Services.AddSingleton(options);
        
        builder.Services.AddSingleton<IAlcedoApplicationInfo, AlcedoApplicationInfo>();

        builder.Services.AddAlcedoCoreInfrastructure(builder.Host, builder.Configuration, builder.Registry,
            builder.WebHostEnvironment, options.Infrastructure);

        builder.Services.AddAlcedoCoreDomain(builder.Registry);

        builder.Services.AddAlcedoCoreApplication();

        builder.Services.AddAlcedoCorePresentation(builder.Registry, options.Presentation);

        if (options.Presentation.AddMvcSupport)
        {
            builder.Services.AddAlcedoCorePresentationMvcSupport();
        }

        builder.Services.AddAlcedoCoreHostEntry(builder.Configuration, builder.Registry, options);

        return builder;
    }

    private static IServiceCollection AddAlcedoCoreHostEntry(this IServiceCollection service,
        IConfiguration configuration, AlcedoModulesRegistry registry, AlcedoCoreOptions options)
    {
        service.AddEndpointsApiExplorer();

        service.AddAlcedoCoreOpenApi(registry.Modules.Assemblies);

        service.AddAlcedoCoreHealthChecks(configuration);

        service.AddProblemDetails();

        service.AddExceptionHandler<AlcedoErrorExceptionHandler>();

        service.AddAlcedoCorsPolicy(configuration, options.Presentation);

        service.AddForwardedHeader(configuration, options.Presentation);

        return service;
    }

    private static IServiceCollection AddForwardedHeader(this IServiceCollection services, IConfiguration configuration, AlcedoPresentationOptions options)
    {
        if (!options.UseForwardedHeaders)
        {
            return services;
        }
        
        var fwdOptions = configuration
            .GetSection(nameof(AlcedoForwardedHeadersOptions))
            .Get<AlcedoForwardedHeadersOptions>() ?? new();
        
        services.Configure<ForwardedHeadersOptions>(opt =>
        {
            opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            opt.KnownProxies.Clear();
            foreach (var proxy in fwdOptions.KnownProxies)
            {
                opt.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            opt.KnownIPNetworks.Clear();
            foreach (var network in fwdOptions.KnownNetworks)
            {
                opt.KnownIPNetworks.Add(
                    new IPNetwork(IPAddress.Parse(network.Prefix), network.PrefixLength));
            }
        });

        return services;
    }


    public static AlcedoBuilder AddModules(this AlcedoBuilder builder)
    {
        foreach (var module in builder.Registry.Modules)
        {
            module.AddServicesToBuilder(builder.Services, builder.Configuration, builder.Registry);
        }

        return builder;
    }
}

public static class AlcedoCoreAppExtension
{
    public static WebApplication UseAlcedoCoreOnStartUp(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<AlcedoCoreOptions>();

        if (options.Infrastructure.WarmUpDbContextOnStartup)
        {
            app.WarmUpDbContext();
        }
        
        app.UseAlcedoCoreStorage();
        app.UseAlcedoAggregateRootGraphCenter();
        app.UseReferenceIntegrityTool(); //Don't delete, otherwise the error won't be thrown during startup
        app.UseEndpointResourceOperationCenter(); //Don't delete, otherwise the error won't be thrown during startup
        
        return app;
    }

    private static WebApplication WarmUpDbContext(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AlcedoDbContext>();
        _ = context.Model;
        return app;
    }

    public static WebApplication UseAlcedoCoreMiddlewares(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<AlcedoCoreOptions>();
        
        if (app.Environment.IsDevelopment())
        {
            app.UseAlcedoCoreOpenApi();
            app.UseAlcedoSmartDoc();
        }
        app.UseSerilogRequestLogging();
        
        app.UseExceptionHandler();

        app.UseAlcedoForwardedHeaders(options);
        
        if (!app.Environment.IsDevelopment() && options.Presentation.UseHsts)
        {
            app.UseHsts();
        }
        
        if (!app.Environment.IsDevelopment() && options.Presentation.UseHttpsRedirection)
        {
            app.UseHttpsRedirection();
        }
        
        if (options.Presentation.AddMvcSupport)
        {
            app.UseAlcedoCorePresentationMvcSupport();
        }
        
        app.UseRouting();
        app.UseAlcedoCorsPolicy();
        return app;
    }

    private static WebApplication UseAlcedoForwardedHeaders(this WebApplication app, AlcedoCoreOptions options)
    {
        if (options.Presentation.UseForwardedHeaders)
        {
            var fwdOptions = app.Services
                .GetRequiredService<IOptions<AlcedoForwardedHeadersOptions>>()
                .Value;

            if (fwdOptions.KnownProxies.Count == 0 &&
                fwdOptions.KnownNetworks.Count == 0)
            {
                app.Logger.LogWarning(
                    "ForwardedHeaders is enabled but no KnownProxies or KnownNetworks are configured. " +
                    "Forwarded headers from reverse proxies may be ignored.");
            }
            
            app.UseForwardedHeaders();
        }
        else
        {
            app.Logger.LogWarning(
                "ForwardedHeaders middleware is disabled. " +
                "This may cause incorrect scheme, host, and client IP detection when running behind a reverse proxy.");
        }

        return app;
    }

    public static WebApplication MapAlcedoCoreControllers(this WebApplication app)
    {
        app.MapControllers();
        app.MapHealthChecks("/health/live", new()
            {
                Predicate = _ => false
            })
            .AllowAnonymous();
        app.MapHealthChecks("/health/ready", new()
            {
                Predicate = r => r.Tags.Contains("ready"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            })
            .AllowAnonymous();
        app.MapGet("/health/info", (IAlcedoApplicationInfo applicationInfo) => Results.Ok(applicationInfo)).AllowAnonymous();
        return app;
    }
}