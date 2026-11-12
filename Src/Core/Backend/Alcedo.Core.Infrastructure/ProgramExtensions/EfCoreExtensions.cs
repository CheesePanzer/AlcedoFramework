using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Core.Infrastructure.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Alcedo.Core.Infrastructure.ProgramExtensions;

internal static class EfCoreExtensions
{
    public static IServiceCollection AddAlcedoCoreDbContext(this IServiceCollection services,
        IConfiguration configuration,
        IReadOnlyCollection<IAlcedoModule> modules,
        IWebHostEnvironment environment,
        AlcedoInfrastructureOptions infrastructureOptions)
    {
        var alcedoColumnEncryptorOptions = configuration
                                               .GetSection(nameof(AlcedoColumnEncryptorOptions))
                                               .Get<AlcedoColumnEncryptorOptions>() ??
                                           throw new InvalidOperationException(
                                               "Missing AlcedoColumnEncryptorOptions in appsettings.json");

        AlcedoColumnEncryptor.Initialize(
            alcedoColumnEncryptorOptions.Key);

        var dataSource = configuration
            .GetSection(nameof(DataSourceOptions))
            .Get<DataSourceOptions>() ?? throw new InvalidOperationException("Missing DataSource in appsettings.json");

        services.AddSingleton<IInterceptor, AggregateSaveChangesInterceptor>();
        services.AddSingleton<IInterceptor, StorageGcInterceptor>();
        services.AddSingleton<IInterceptor, PostgresExceptionInterceptor>();

        services.AddDbContext<AlcedoDbContext>((sp, options) =>
        {
            options.UseNpgsql(dataSource.Postgres,
                o =>
                {
                    if (infrastructureOptions.MigrationAssembly is not null)
                    {
                        o.MigrationsAssembly(infrastructureOptions.MigrationAssembly);
                    }
                }
            );

            options.ReplaceService<IModelCustomizer, AlcedoModelCustomizer>();

            ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(
                new AlcedoOptionsExtension(modules, infrastructureOptions));

            var interceptors = sp.GetServices<IInterceptor>();
            options.AddInterceptors(interceptors);

            if (environment.IsDevelopment())
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }

            options.UseSnakeCaseNamingConvention();
        });

        return services;
    }
}