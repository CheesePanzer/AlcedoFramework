using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Abstractions;

/// <summary>
/// Defines the contract for a vertically-sliced business module.
/// Each module is responsible for its own service registration, middleware, and endpoint mapping.
/// The framework uses the module list to coordinate assembly scanning, license validation,
/// and lifecycle management.
/// </summary>
public interface IAlcedoModule
{
    /// <summary>
    /// Display Code of the module.
    /// </summary>
    string ModuleCode { get; }

    /// <summary>
    /// Display name of the module.
    /// </summary>
    string ModuleName { get; }
    
    string ModuleVersion { get; }

    /// <summary>
    /// The assembly containing this module's types.
    /// Used by CoreInfra for EF Core configuration scanning and Mapster profile registration.
    /// </summary>
    Assembly Assembly { get; }

    /// <summary>
    /// Register services into the DI container.
    /// Called during the service registration phase before the application is built.
    /// </summary>
    void AddServicesToBuilder(IServiceCollection services, IConfiguration configuration,
        AlcedoModulesRegistry registry);
}

public abstract class AlcedoModuleBase : IAlcedoModule
{
    public virtual string ModuleCode { get; }
    public virtual string ModuleName { get; }
    public string ModuleVersion { get; }
    public Assembly Assembly { get; }

    protected AlcedoModuleBase(string? moduleCode = null, string? moduleName = null)
    {
        var type = GetType();
        var defaultName = type.Name.ToLowerInvariant().Replace("module", "");

        ModuleCode = moduleCode ?? defaultName;
        ModuleName = moduleName ?? defaultName;

        Assembly = type.Assembly;
        ModuleVersion = Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion 
                        ?? Assembly.GetName().Version?.ToString()
                        ?? "0.0.0";
    }

    public void AddServicesToBuilder(IServiceCollection services, IConfiguration configuration,
        AlcedoModulesRegistry registry)
    {
        ScanAndRegisterDiServiceInLocalAssembly(services);
        AddServicesManually(services, configuration, registry);
    }

    protected virtual void AddServicesManually(IServiceCollection services, IConfiguration configuration,
        AlcedoModulesRegistry registry)
    {
    }

    private void ScanAndRegisterDiServiceInLocalAssembly(IServiceCollection services) =>
        AutoServiceRegister.AutoRegisterInAssembly(services, Assembly);
}