using System.Reflection;
using Alcedo.Framework.FormulaEngine.Contract.Abstraction;
using Alcedo.Framework.FormulaEngine.Domain;

namespace Alcedo.Framework.FormulaEngine.EngineCore;

public static class FormulaParamFlattener
{
    public static IReadOnlyList<ParameterCreateCommand> FlattenInput<TInput>()
        => Flatten(typeof(TInput), prefix: "In_");

    public static IReadOnlyList<ParameterCreateCommand> FlattenOutput<TOutput>()
        => Flatten(typeof(TOutput), prefix: string.Empty);
    
    public static IReadOnlyList<ParameterCreateCommand> FlattenInput(Type inputType)
        => Flatten(inputType, prefix: "In_");

    public static IReadOnlyList<ParameterCreateCommand> FlattenOutput(Type outputType)
        => Flatten(outputType, prefix: string.Empty);

    private static List<ParameterCreateCommand> Flatten(Type type, string prefix)
    {
        var results = new List<ParameterCreateCommand>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<ParamAttribute>();
            var segmentName = attr?.VariableName ?? prop.Name;
            var fullName = prefix + segmentName;
            var propType = prop.PropertyType;

            if (IsPrimitive(propType))
            {
                results.Add(new ParameterCreateCommand(fullName, GetTypeName(propType), attr?.Description));
            }
            else if (attr?.VariableName is not null)
            {
                throw new InvalidOperationException(
                    $"Property '{prop.Name}' is a complex type but has a VariableName override '{attr.VariableName}'. " +
                    $"Only primitive types can have VariableName overrides.");
            }
            else
            {
                results.AddRange(Flatten(propType, fullName + "_"));
            }
        }

        return results;
    }

    public static bool IsPrimitive(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t == typeof(decimal) || t == typeof(string) || t == typeof(DateTime);
    }

    private static string GetTypeName(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.Name.ToLowerInvariant();
    }
}