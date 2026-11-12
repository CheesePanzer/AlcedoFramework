using System.Reflection;
using Alcedo.Core.Abstractions;

namespace Alcedo.Core.Host;

public static class VersionExtensions
{
    /// <summary>
    /// Simplifies a version string containing Build Metadata (e.g., 1.0.0+6ea3707f...) into a UI-friendly format.
    /// </summary>
    /// <param name="rawVersion">The raw version string.</param>
    /// <param name="hashLength">The number of Git Hash characters to retain (defaults to 7).</param>
    /// <param name="includeHash">Whether to append the short hash (if false, returns only the SemVer e.g., 1.0.0).</param>
    /// <returns>A formatted short version string.</returns>
    public static string ToShortVersion(this string? rawVersion, int hashLength = 7, bool includeHash = true)
    {
        if (string.IsNullOrWhiteSpace(rawVersion))
            return "0.0.0";

        var parts = rawVersion.Split('+');
        var semVer = parts[0];

        if (includeHash && parts.Length > 1 && parts[1].Length >= hashLength)
        {
            var shortHash = parts[1][..hashLength];
            return $"{semVer} ({shortHash})"; // Output: 1.0.0 (6ea3707)
        }

        return semVer;
    }
}

public interface IAlcedoApplicationInfo
{
    string CoreVersion { get; }
    string AppVersion { get; }
    
    IReadOnlyDictionary<string, string>  ModuleVersions { get; }
}

public record AlcedoApplicationInfo : IAlcedoApplicationInfo
{
    public AlcedoApplicationInfo(AlcedoModulesRegistry registry)
    {
        var coreAssembly = typeof(AlcedoApplicationInfo).Assembly;
        CoreVersion = coreAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion 
                      ?? coreAssembly.GetName().Version?.ToString()
                      ?? "0.0.0";
        
        var entryAssembly = Assembly.GetEntryAssembly();
        AppVersion = entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                     ?? entryAssembly?.GetName().Version?.ToString()
                     ?? "0.0.0";

        ModuleVersions = registry.Modules.Select(m => (m.ModuleCode, m.ModuleVersion)).ToDictionary();
    }

    public string CoreVersion { get; init; }
    public string AppVersion { get; init; }
    
    public IReadOnlyDictionary<string, string> ModuleVersions { get; init; }
}