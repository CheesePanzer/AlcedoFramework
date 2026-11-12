using System.Reflection;
using Alcedo.Core.Application;
using Alcedo.Core.Infrastructure.Redis;
using Alcedo.Core.Infrastructure.Redis.KeySpace;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Alcedo.Core.Infrastructure.ProgramExtensions;

internal static class RedisExtensions
{
    public static IServiceCollection AddAlcedoRedis(this IServiceCollection services, IConfiguration configuration,
        IReadOnlyCollection<Assembly> assemblies)
    {
        var dataSourceOptions = configuration
            .GetSection(nameof(DataSourceOptions))
            .Get<DataSourceOptions>() ?? throw new InvalidOperationException("Missing DataSource in appsettings.json");

        // Redis Connection
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisConfig = dataSourceOptions.Redis;
            var options = ConfigurationOptions.Parse(redisConfig);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options ??
                                                 throw new NullReferenceException("Missing Redis Connection String"));
        });


        services.ScanAndRegisterRedisKeySpace(assemblies);

        services.AddSingleton<IDistributedReaderWriterLockProvider>(sp =>
        {
            var database = sp.GetRequiredService<DistributedLockRedisKeySpace>();
            return new RedisDistributedSynchronizationProvider(database.Db);
        });

        return services;
    }

    private static IServiceCollection ScanAndRegisterRedisKeySpace(this IServiceCollection services,
        IReadOnlyCollection<Assembly> assemblies)
    {
        var databaseTypes = assemblies
            .Append(Assembly.GetExecutingAssembly())
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(RedisKeySpaceBase)));

        HashSet<string> redisKeySpaces = new HashSet<string>();

        foreach (var type in databaseTypes)
        {
            if (!type.Name.EndsWith("RedisKeySpace"))
            {
                throw new InvalidOperationException($"{type.Name} is not end with prefix : RedisKeySpace");
            }

            if (!redisKeySpaces.Add(type.Name))
            {
                throw new InvalidOperationException($"{type.Name} is already in RedisKeySpace");
            }

            services.AddSingleton(type);

            var attr = type.GetCustomAttribute<RegisterAsEntityCacheStoreAttribute>();

            if (attr == null)
            {
                continue;
            }

            var dtoType = attr.DtoType;

            var interfaceType = typeof(IEntityCacheStore<>).MakeGenericType(dtoType);
            var implementationType = typeof(EntityCacheRedisStore<,>).MakeGenericType(type, dtoType);

            services.AddSingleton(interfaceType, implementationType);
        }

        return services;
    }
}