using System.Reflection;
using Alcedo.Core.Infrastructure.EfCore;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Infrastructure.ProgramExtensions;

internal static class MassTransitExtensions
{
    public static IServiceCollection AddAlcedoMassTransitPublic(this IServiceCollection services,
        IConfiguration configuration, IReadOnlyCollection<Assembly> assemblies, AlcedoInfrastructureOptions alcedoInfrastructureOptions)
    {
        if (alcedoInfrastructureOptions.UsePostgresMqTransfer)
        {
            services.AddAlcedoMassTransitWithPostgres(configuration, assemblies);
        }
        else
        {
            services.AddAlcedoMassTransitWithRabbitMq(configuration, assemblies);
        }
        
        return services;
    }
    
    public static IServiceCollection AddAlcedoMassTransitWithRabbitMq(this IServiceCollection services,
        IConfiguration configuration, IReadOnlyCollection<Assembly> assemblies)
    {
        var endpointConfigurations = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsAssignableTo(typeof(IAlcedoRabbitMqReceiveEndpointConfiguration))
                        && t is { IsAbstract: false, IsInterface: false })
            .Select(t => (IAlcedoRabbitMqReceiveEndpointConfiguration)Activator.CreateInstance(t)!)
            .ToList();

        services.AddMassTransit(x =>
        {
            foreach (var assembly in assemblies)
            {
                x.AddConsumers(assembly);
            }

            x.AddEntityFrameworkOutbox<AlcedoDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });
            
            x.AddConfigureEndpointsCallback((context, name, cfg) =>
            {
                cfg.UseEntityFrameworkOutbox<AlcedoDbContext>(context);
            });

            var rabbitMqSection = configuration
                                      .GetSection(nameof(DataSourceOptions))
                                      .Get<DataSourceOptions>()?.RabbitMQ ??
                                  throw new InvalidOperationException("Missing DataSource in appsettings.json");

            var msgQueueOptions = configuration
                                      .GetSection(nameof(MsgQueueOptions))
                                      .Get<MsgQueueOptions>() ??
                                  throw new InvalidOperationException("Missing MsgQueueOptions in appsettings.json");

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqSection.Host, rabbitMqSection.Port,
                    rabbitMqSection.VirtualHost, h =>
                    {
                        h.Username(rabbitMqSection.Username);
                        h.Password(rabbitMqSection.Password);
                    });

                cfg.UseMessageRetry(r =>
                    r.Interval(msgQueueOptions.RetryCount, TimeSpan.FromSeconds(msgQueueOptions.RetryIntervalSecs)));

                cfg.PrefetchCount = msgQueueOptions.PrefetchCount;
                cfg.ConcurrentMessageLimit = msgQueueOptions.ConcurrentMessageLimit;

                foreach (var endpointConfig in endpointConfigurations)
                {
                    endpointConfig.Configure(cfg, context);
                }

                cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(msgQueueOptions.ServiceName, false));
            });
        });

        return services;
    }
    
    internal static IServiceCollection AddAlcedoMassTransitWithPostgres(this IServiceCollection services,
        IConfiguration configuration, IReadOnlyCollection<Assembly> assemblies)
    {
        var endpointConfigurations = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsAssignableTo(typeof(IAlcedoPostgresMqReceiveEndpointConfiguration))
                        && t is { IsAbstract: false, IsInterface: false })
            .Select(t => (IAlcedoPostgresMqReceiveEndpointConfiguration)Activator.CreateInstance(t)!)
            .ToList();
        
        var dataSource = configuration
            .GetSection(nameof(DataSourceOptions))
            .Get<DataSourceOptions>() ?? throw new InvalidOperationException("Missing DataSource in appsettings.json");

        services.AddOptions<SqlTransportOptions>().Configure(options =>
        {
            options.ConnectionString = dataSource.Postgres;
        });

        services.AddPostgresMigrationHostedService();

        services.AddMassTransit(x =>
        {
            foreach (var assembly in assemblies)
            {
                x.AddConsumers(assembly);
            }

            x.AddEntityFrameworkOutbox<AlcedoDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });
            
            x.AddConfigureEndpointsCallback((context, name, cfg) =>
            {
                cfg.UseEntityFrameworkOutbox<AlcedoDbContext>(context);
            });

            x.UsingPostgres((context, cfg) =>
            {
                var msgQueueOptions = configuration
                                          .GetSection(nameof(MsgQueueOptions))
                                          .Get<MsgQueueOptions>() ??
                                      throw new InvalidOperationException("Missing MsgQueueOptions in appsettings.json");

                cfg.UseMessageRetry(r =>
                    r.Interval(msgQueueOptions.RetryCount, TimeSpan.FromSeconds(msgQueueOptions.RetryIntervalSecs)));

                foreach (var endpointConfig in endpointConfigurations)
                {
                    endpointConfig.Configure(cfg, context);
                }

                cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(msgQueueOptions.ServiceName, false));
            });
        });

        return services;
    }
}