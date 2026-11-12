using System.Reflection;
using System.Text;
using Alcedo.Core.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Alcedo.Core.Plugin.SmartDoc;

public record AlcedoSmartDoc(string Content);

public static class AlcedoCorePluginSmartDoc
{
    public static IApplicationBuilder UseAlcedoSmartDoc(this IApplicationBuilder app)
    {
        var registry = app.ApplicationServices.GetRequiredService<AlcedoModulesRegistry>();

        var doc = Generate(registry.Modules.Assemblies);

        app = app.UseMiddleware<SmartDocMiddleware>(doc, "/smartDoc.json");

        return app.UseMiddleware<SmartDocUiMiddleware>("/smartDoc", "/smartDoc.json");
    }

    private static string Generate(IReadOnlyCollection<Assembly> assemblies)
    {
        var aggList = AggregateRootListDocGenerator.GenerateJson();

        var refList = AggregateRootReferenceRelationsDocGenerator.GenerateJson();

        var appSettings = AppSettingsDemoScanner.Generate(assemblies);

        return $$"""
                 {
                   "AggregateRoots": {{aggList}},
                   "ReferenceRelations": {{refList}},
                   "AppSettings": {{appSettings.TrimEnd(",")}}
                 }
                 """;
    }
}