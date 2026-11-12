using Alcedo.Core.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Host;

public static partial class ProgramExtensions
{
    public static IServiceCollection AddAlcedoCoreHealthChecks(this IServiceCollection services,
        IConfiguration configuration)
    {
        var dataSource = configuration
            .GetSection(nameof(DataSourceOptions))
            .Get<DataSourceOptions>() ?? throw new InvalidOperationException("Missing DataSource in appsettings.json");

        services.AddHealthChecks() //mass transit already has its own endpoint
            .AddNpgSql(
                connectionString: dataSource.Postgres,
                name: "postgresql",
                tags: new[] { "db", "postgres", "ready" })
            .AddRedis(
                redisConnectionString: dataSource.Redis,
                name: "redis",
                tags: new[] { "cache", "redis", "ready" });

        return services;
    }
}