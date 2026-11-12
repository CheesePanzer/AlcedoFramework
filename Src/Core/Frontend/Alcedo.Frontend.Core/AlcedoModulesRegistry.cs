using System.Reflection;

namespace Alcedo.Frontend.Core;

public sealed class AlcedoModulesRegistry
{
    private readonly List<IAlcedoModule> _modules = [];

    public IReadOnlyList<IAlcedoModule> Modules => _modules;

    private readonly Dictionary<Assembly, IAlcedoModule> _assemblyLookup = new();

    public AlcedoModulesRegistry()
    {
    }

    public AlcedoModulesRegistry AddModule(IAlcedoModule module)
    {
        if (_assemblyLookup.ContainsKey(module.Assembly))
        {
            throw new InvalidOperationException(
                $"Current Module: {module.ModuleName} With Assembly: {module.Assembly} Already registered;");
        }

        _modules.Add(module);
        _assemblyLookup[module.Assembly] = module;
        return this;
    }

    public AlcedoModulesRegistry AddModules(params IAlcedoModule[] modules)
    {
        foreach (var module in modules)
        {
            AddModule(module);
        }

        return this;
    }

    public AlcedoModulesRegistry AddModules(IEnumerable<IAlcedoModule> modules)
    {
        foreach (var module in modules)
        {
            AddModule(module);
        }

        return this;
    }

    public string? GetModuleCodeByAssembly(Assembly assembly)
    {
        return _assemblyLookup.TryGetValue(assembly, out var module)
            ? module.ModuleCode
            : null;
    }
}

public static class AlcedoModuleListExtensions
{
    extension(IReadOnlyCollection<IAlcedoModule> modules)
    {
        public IReadOnlyCollection<Assembly> Assemblies => modules.Select(m => m.Assembly).Distinct().ToList();
    }
}