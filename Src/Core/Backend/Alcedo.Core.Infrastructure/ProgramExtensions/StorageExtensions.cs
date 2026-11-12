using Alcedo.Core.Domain.IStorageService;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Core.Infrastructure.Storage;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Infrastructure.ProgramExtensions;

public static class StorageExtensions
{
    public static IServiceCollection AddAlcedoCoreStorage(this IServiceCollection services,
        IConfiguration configuration)
    {
        var awsOptions = configuration.GetAWSOptions();

        var section = configuration.GetSection("AwsCredentials");
        awsOptions.Credentials = new BasicAWSCredentials(
            section["AccessKey"],
            section["SecretKey"]
        );

        services.AddAWSService<IAmazonS3>(awsOptions);

        services.AddSingleton<IFileStorageService, S3FileStorageService>();

        return services;
    }

    public static IApplicationBuilder UseAlcedoCoreStorage(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AlcedoDbContext>();
        StorageKeyCacheData.Initialize(dbContext.Model);
        return app;
    }
}