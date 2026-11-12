using System.Reflection;
using System.Text.Json;
using Alcedo.Framework.FormulaEngine.EngineCore;

namespace Alcedo.Framework.FormulaEngine.Application.Helpers;

public static class TestFormulaHelper
{
    public static string BuildInputTemplate(Type type)
        => JsonSerializer.Serialize(BuildJsonTemplate(type));
    
    public static object? BuildJsonTemplate(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (FormulaParamFlattener.IsPrimitive(underlying))
        {
            return GetDefaultPlaceholder(underlying);
        }

        var result = new Dictionary<string, object?>();

        foreach (var prop in underlying.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            result[prop.Name] = BuildJsonTemplate(prop.PropertyType);
        }

        return result;
    }

    private static object? GetDefaultPlaceholder(Type type) => type switch
    {
        _ when type == typeof(decimal) => 0m,
        _ when type == typeof(int) => 0,
        _ when type == typeof(string) => "",
        _ when type == typeof(bool) => false,
        _ when type == typeof(DateTime) => DateTime.UtcNow,
        _ => null
    };
}