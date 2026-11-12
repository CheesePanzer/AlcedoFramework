using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Alcedo.Framework.Notification.Contracts;

public static class MessageExampleGenerator
{
    public static string GenerateJsonExample(Type messageType)
    {
        var jsonNode = BuildExampleNode(messageType, new HashSet<Type>());
        return jsonNode.ToJsonString(new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    private static JsonNode BuildExampleNode(Type type, HashSet<Type> visitedTypes)
    {
        // 处理基础数据类型
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        if (underlyingType == typeof(string)) return "string";
        if (underlyingType == typeof(int) || underlyingType == typeof(long)) return 0;
        if (underlyingType == typeof(decimal) || underlyingType == typeof(double) || underlyingType == typeof(float)) return 0.00m;
        if (underlyingType == typeof(bool)) return true;
        if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset)) return DateTimeOffset.UtcNow.ToString("o");
        if (underlyingType == typeof(Guid)) return Guid.Empty.ToString();
        if (underlyingType.IsEnum) return Enum.GetNames(underlyingType).FirstOrDefault() ?? "0";

        // 防止循环引用
        if (!visitedTypes.Add(type)) return "{}";

        // 处理集合/数组类型
        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            var elementType = type.IsGenericType 
                ? type.GetGenericArguments()[0] 
                : (type.GetElementType() ?? typeof(object));
            
            var array = new JsonArray();
            array.Add(BuildExampleNode(elementType, new HashSet<Type>(visitedTypes)));
            return array;
        }

        // 处理复杂对象 (Class / Struct)
        var obj = new JsonObject();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.CanWrite || HasMatchingCtorParam(type, p.Name));

        foreach (var prop in properties)
        {
            obj[prop.Name] = BuildExampleNode(prop.PropertyType, new HashSet<Type>(visitedTypes));
        }

        return obj;
    }

    private static bool HasMatchingCtorParam(Type type, string propName)
    {
        return type.GetConstructors()
                   .SelectMany(c => c.GetParameters())
                   .Any(p => string.Equals(p.Name, propName, StringComparison.OrdinalIgnoreCase));
    }
}