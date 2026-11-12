using System.Reflection;
using Alcedo.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Infrastructure.ProgramExtensions;

internal static class AppSettingsRegister
{
    public static void BindingAppSettingsToOptions(this IServiceCollection services, IConfiguration configuration,
        IReadOnlyCollection<Assembly> assemblies)
    {
        RegisterEachAssembly(services, configuration, Assembly.GetExecutingAssembly());
        foreach (var assembly in assemblies)
        {
            RegisterEachAssembly(services, configuration, assembly);
        }
    }

    private static void RegisterSetting<T>(IServiceCollection services, IConfigurationSection section)
        where T : class
    {
        services.AddOptions<T>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void RegisterEachAssembly(IServiceCollection services, IConfiguration configuration,
        Assembly assembly)
    {
        var settingsTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AppSettingsBase)));

        var registerMethod = typeof(AppSettingsRegister)
            .GetMethod(nameof(RegisterSetting), BindingFlags.NonPublic | BindingFlags.Static)!;

        foreach (var type in settingsTypes)
        {
            string sectionName = type.Name;
            var section = configuration.GetSection(sectionName);

            if (!section.Exists())
            {
                throw new InvalidOperationException(
                    $"[Configuration Error]: Section '{sectionName}' not found. Please verify that the key exists in appsettings, or check the naming and attributes of class {type.Name}.");
            }

            registerMethod.MakeGenericMethod(type).Invoke(null, [services, section]);
        }
    }
}