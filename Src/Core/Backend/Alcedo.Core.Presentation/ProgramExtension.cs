using System.Reflection;
using System.Text.Json.Serialization;
using Alcedo.Core.Abstractions;
using Alcedo.Core.Presentation.Filters;
using Alcedo.Core.Presentation.PreException;
using Alcedo.Core.Presentation.RequestJsonConverter;
using Alcedo.Core.Presentation.ValueAtomModel;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Presentation;

internal static class ProgramExtension
{
    public static IServiceCollection AddAlcedoCorePresentation(this IServiceCollection services,
        AlcedoModulesRegistry registry, AlcedoPresentationOptions options)
    {
        services.AddSingleton<EndpointResourceOperationCenter>(_ =>
            new EndpointResourceOperationCenter(registry.Modules.Assemblies));
        services.AddAlcedoCoreControllers(registry,options.AdditionalGlobalFilters);
        AlcedoProblemDetailsFactory.DisplaySensitiveExceptions = options.DisplaySensitiveExceptions;
        return services;
    }

    private static IServiceCollection AddAlcedoCoreControllers(this IServiceCollection services,
        AlcedoModulesRegistry registry, IReadOnlyList<IFilterMetadata> addtionalFilters)
    {
        services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });

        var controllerBuilder = services.AddControllers(options =>
            {
                options.Filters.Add<RequestIdempotencyKeyFilter>();
                foreach (var filter in addtionalFilters)
                {
                    options.Filters.Add(filter);
                }
                options.ModelBinderProviders.Insert(0, new ValueAtomModelBinderProvider());
                options.ModelValidatorProviders.Insert(0, new AlcedoValidatorProvider());
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .Select(e => $"{e.Key}: {string.Join(", ", e.Value!.Errors.Select(x => x.ErrorMessage))}")
                        .ToList();

                    var detail = string.Join(" | ", errors);

                    throw new PreErrRequestInvalid(
                        detail
                    );
                };
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter()); // enum display string in json
                options.JsonSerializerOptions.Converters.Add(
                    new JsonEmptyStringToNullConverter()); // empty string is null
                options.JsonSerializerOptions.Converters.Add(
                    new ValueAtomConverterFactory()); // valueatom to string
            });

        foreach (var assembly in registry.Modules.Assemblies)
        {
            controllerBuilder.AddApplicationPart(assembly);
        }

        services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("x-api-version"),
                new QueryStringApiVersionReader("api-version")
            );
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    public static IApplicationBuilder UseEndpointResourceOperationCenter(this IApplicationBuilder app)
    {
        _ = app.ApplicationServices.GetRequiredService<EndpointResourceOperationCenter>();

        return app;
    }
}