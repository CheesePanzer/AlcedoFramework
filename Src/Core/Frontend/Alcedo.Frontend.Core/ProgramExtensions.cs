using System.Reflection;
using Alcedo.Frontend.Abstraction;
using Alcedo.Frontend.Core.HttpClient;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Frontend.Core;

public sealed class AlcedoFrontendCoreOptions
{
    public List<Type>? AdditionalClientHandlers { get; set; }
}

internal static class ProgramExtensions
{
    public static WebAssemblyHostBuilder AddAlcedoFrontEndCore(this WebAssemblyHostBuilder builder,
        AlcedoModulesRegistry registry, AlcedoFrontendCoreOptions options)
    {
        var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
                         ?? throw new InvalidOperationException("ApiBaseUrl is not configured");
        builder.Services.AddSingleton(registry);
        builder.Services.AddSingleton<IAlcedoApplicationInfo, AlcedoApplicationInfo>();
        builder.Services.AddHttpClient<AlcedoHealthClient>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        builder.AddAlcedoDispatchClient(apiBaseUrl, registry.Modules.Assemblies,
            options.AdditionalClientHandlers);
        foreach (var module in registry.Modules)
        {
            module.AddServicesToBuilder(builder);
        }

        return builder;
    }

    private static WebAssemblyHostBuilder AddAlcedoDispatchClient(this WebAssemblyHostBuilder builder,
        string apiBaseUrl,
        IReadOnlyCollection<Assembly> assemblies,
        IReadOnlyCollection<Type>? additionalHandlers = null)
    {
        builder.Services.AddScoped<IdempotencyHandler>();
        builder.Services.AddScoped<ApiErrorHandler>();
        builder.Services.AddScoped<CredentialsHandler>();

        var clientInterfaces = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsInterface && t.GetCustomAttribute<ApiClientAttribute>() is not null);

        if (additionalHandlers is not null)
        {
            foreach (var handlerType in additionalHandlers)
            {
                builder.Services.AddScoped(handlerType);
            }
        }

        var addHttpMessageHandlerMethod = typeof(HttpClientBuilderExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(HttpClientBuilderExtensions.AddHttpMessageHandler)
                        && m.IsGenericMethod
                        && m.GetGenericArguments().Length == 1
                        && m.GetParameters().Length == 1);

        foreach (var interfaceType in clientInterfaces)
        {
            var name = interfaceType.Name;

            var httpClientBuilder = builder.Services
                .AddHttpClient(name, c => c.BaseAddress = new Uri(apiBaseUrl));

            httpClientBuilder
                .AddHttpMessageHandler<CredentialsHandler>()
                .AddHttpMessageHandler<IdempotencyHandler>()
                .AddHttpMessageHandler<ApiErrorHandler>();

            if (additionalHandlers is not null)
            {
                foreach (var handlerType in additionalHandlers)
                {
                    var genericMethod = addHttpMessageHandlerMethod.MakeGenericMethod(handlerType);

                    genericMethod.Invoke(null, [httpClientBuilder]);
                }
            }

            builder.Services.AddScoped(interfaceType, sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var http = factory.CreateClient(name);

                return typeof(AlcedoDispatchProxy)
                    .GetMethod(nameof(AlcedoDispatchProxy.Create))!
                    .MakeGenericMethod(interfaceType)
                    .Invoke(null, [http])!;
            });
        }

        return builder;
    }
}