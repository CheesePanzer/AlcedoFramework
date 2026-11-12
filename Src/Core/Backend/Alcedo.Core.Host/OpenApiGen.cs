using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace Alcedo.Core.Host;

public static partial class ProgramExtensions
{
    public static IServiceCollection AddAlcedoCoreOpenApi(
        this IServiceCollection services,
        IReadOnlyCollection<Assembly> assemblies)
    {
        var docTransformerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IOpenApiDocumentTransformer).IsAssignableFrom(t)
                        && t is { IsInterface: false, IsAbstract: false });

        var opTransformerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IOpenApiOperationTransformer).IsAssignableFrom(t)
                        && t is { IsInterface: false, IsAbstract: false });

        services.AddOpenApi(options =>
        {
            options.AddSchemaTransformer<ValueAtomSchemaTransformer>();

            options.AddSchemaTransformer<EnumDisplayStringTransformer>();

            options.AddSchemaTransformer<NumberTypeSchemaTransformer>();

            options.AddOperationTransformer<ValueAtomParameterTransformer>();

            options.AddOperationTransformer<IdempotencyKeyTransformer>();

            var docMethod =
                typeof(OpenApiOptions).GetMethod(nameof(OpenApiOptions.AddDocumentTransformer), Type.EmptyTypes);

            foreach (var type in docTransformerTypes)
            {
                var genericMethod = docMethod?.MakeGenericMethod(type);
                genericMethod?.Invoke(options, null);
            }

            var opMethod =
                typeof(OpenApiOptions).GetMethod(nameof(OpenApiOptions.AddOperationTransformer), Type.EmptyTypes);

            foreach (var type in opTransformerTypes)
            {
                var genericMethod = opMethod?.MakeGenericMethod(type);
                genericMethod?.Invoke(options, null);
            }
        });

        return services;
    }

    public static IApplicationBuilder UseAlcedoCoreOpenApi(this WebApplication app)
    {
        app.MapOpenApi()
            .AllowAnonymous();
        app.MapScalarApiReference()
            .AllowAnonymous();
        return app;
    }
}