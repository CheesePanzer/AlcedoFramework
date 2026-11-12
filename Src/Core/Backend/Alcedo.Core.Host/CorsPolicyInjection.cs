using Alcedo.Core.Infrastructure;
using Alcedo.Core.Presentation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alcedo.Core.Host;

public static partial class ProgramExtensions
{
    public static IServiceCollection AddAlcedoCorsPolicy(this IServiceCollection services,
        IConfiguration configuration, AlcedoPresentationOptions options)
    {
        if (!options.UseCors)
        {
            return services;
        }
        
        var corsOptions = configuration
                              .GetSection(nameof(AlcedoCorsOptions))
                              .Get<AlcedoCorsOptions>() ??
                          throw new InvalidOperationException("Missing AlcedoCorsOptions in appsettings.json");
        
        services.AddCors(opt =>
        {
            opt.AddDefaultPolicy(policyBuilder =>
            {
                policyBuilder
                    .WithOrigins(corsOptions.AllowedOrigins.ToArray())
                    .WithHeaders(corsOptions.AllowedHeaders.ToArray())
                    .WithMethods(corsOptions.AllowedMethods.ToArray())
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static WebApplication UseAlcedoCorsPolicy(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<AlcedoCoreOptions>();
        if (!options.Presentation.UseCors)
        {
            return app;
        }
        
        var corsOptions = app.Services
            .GetRequiredService<IOptions<AlcedoCorsOptions>>()
            .Value;

        if (!corsOptions.AllowedOrigins.Any())
        {
            app.Logger
                .LogWarning(
                    "CORS is enabled but AlcedoCorsOptions.AllowedOrigins is empty.");
        }

        if (!corsOptions.AllowedHeaders.Any())
        {
            app.Logger.LogWarning(
                "CORS is enabled but AlcedoCorsOptions.AllowedHeaders is empty.");
        }

        if (!corsOptions.AllowedMethods.Any())
        {
            app.Logger.LogWarning(
                "CORS is enabled but AlcedoCorsOptions.AllowedMethods is empty.");
        }
        
        app.UseCors();
        return app;
    }
}