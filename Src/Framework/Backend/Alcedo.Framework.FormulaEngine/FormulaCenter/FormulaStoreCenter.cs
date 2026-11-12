using System.Reflection;
using Alcedo.Framework.FormulaEngine.Contract.Abstraction;

namespace Alcedo.Framework.FormulaEngine.FormulaCenter;

public sealed record FormulaMetaData
{
    public FormulaMetaData(string code, string name, string description, Type formulaType, Type inputType, Type outputType, bool isObsolete = false, string? obsoleteReason = null)
    {
        Code = code;
        Name = name;
        Description = description;
        IsObsolete = isObsolete;
        FormulaType = formulaType;
        ObsoleteReason = obsoleteReason;
        InputType = inputType;
        OutputType = outputType;
    }

    public string Code { get; init; }
    
    public string Name { get; init; }
    
    public string Description { get; init; }
    
    public bool IsObsolete { get; init; }
    
    public string? ObsoleteReason { get; init; }
    
    public Type FormulaType { get; init; }
    
    public Type InputType { get; init; }
    
    public Type OutputType { get; init; }
}

public static class FormulaStoreCenter
{
    private static List<FormulaMetaData> _metaData = new List<FormulaMetaData>();
    
    private static Dictionary<string, FormulaMetaData> _codeKeyMetaData = new Dictionary<string, FormulaMetaData>();
    
    private static Dictionary<Type, FormulaMetaData> _typeKeyMetaData = new Dictionary<Type, FormulaMetaData>();

    public static void OnInitialize(IReadOnlyCollection<Assembly> assemblies)
    {
        if (assemblies.Count == 0)
        {
            return;
        }

        HashSet<string> codeSet = new HashSet<string>();

        HashSet<Type> typeSet = new HashSet<Type>();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes();
            
            foreach (var type in types)
            {
                if (!type.IsClass || type.IsAbstract || !IsSubclassOfRawGeneric(typeof(Formula<,>), type, out var baseType))
                {
                    continue;
                }
                
                var genericArguments = baseType!.GetGenericArguments();
                var inputType = genericArguments[0];
                var outputType = genericArguments[1];
                
                var formulaAttr = type.GetCustomAttribute<FormulaAttribute>();
                var obsoleteAttr = type.GetCustomAttribute<ObsoleteAttribute>();
                
                bool isObsolete = obsoleteAttr != null;
                string? obsoleteReason = obsoleteAttr?.Message;
                
                string code = !string.IsNullOrWhiteSpace(formulaAttr?.Code) ? formulaAttr.Code : type.Name;
                string name = !string.IsNullOrWhiteSpace(formulaAttr?.Name) ? formulaAttr.Name : code;
                string description = !string.IsNullOrWhiteSpace(formulaAttr?.Description) ? formulaAttr.Description : "-";

                if (!typeSet.Add(type))
                {
                    throw new Exception($"Formula {type} is already existed");
                }

                if (!codeSet.Add(code))
                {
                    throw new Exception($"Formula {type}: the code {code} is already in use");
                }

                var meta = new FormulaMetaData(
                    code: code,
                    name: name,
                    description: description,
                    formulaType: type,
                    inputType: inputType,
                    outputType: outputType,
                    isObsolete: isObsolete,
                    obsoleteReason: obsoleteReason
                );

                _metaData.Add(meta);
            }
        }
        
        _codeKeyMetaData = _metaData.ToDictionary(k => k.Code, v => v, StringComparer.OrdinalIgnoreCase);
        _typeKeyMetaData = _metaData.ToDictionary(k => k.FormulaType, v => v);
    }

    private static bool IsSubclassOfRawGeneric(Type generic, Type? toCheck, out Type? implementingBaseType)
    {
        while (toCheck != null && toCheck != typeof(object))
        {
            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (generic == cur)
            {
                implementingBaseType = toCheck;
                return true;
            }
            toCheck = toCheck.BaseType;
        }
        implementingBaseType = null;
        return false;
    }
    
    public static FormulaMetaData? GetByCode(string code) => _codeKeyMetaData.TryGetValue(code, out var meta) ? meta : null;
    
    public static FormulaMetaData? GetByType<TFormula>() => _typeKeyMetaData.TryGetValue(typeof(TFormula), out var meta) ? meta : null;
    
    public static FormulaMetaData? GetByType(Type type) => _typeKeyMetaData.TryGetValue(type, out var meta) ? meta : null;
    public static IReadOnlyCollection<FormulaMetaData> GetAll() => _metaData.AsReadOnly();
}