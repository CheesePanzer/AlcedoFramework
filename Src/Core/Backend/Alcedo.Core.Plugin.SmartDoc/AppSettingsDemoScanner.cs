using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure;

namespace Alcedo.Core.Plugin.SmartDoc;

public static class AppSettingsDemoScanner
{
    public static string Generate(IEnumerable<Assembly> assemblies)
    {
        var newAssemblies = assemblies.ToList();
        newAssemblies.Add(typeof(AlcedoCoreInfrastructureComponent).Assembly);
        var settingsTypes = newAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AppSettingsBase)))
            .Distinct()
            .OrderBy(t => t.Name)
            .ToList();

        var root = new DemoObject();
        foreach (var type in settingsTypes)
        {
            var section = BuildObject(type, new HashSet<Type>());
            if (section?.Properties.Count > 0)
            {
                root.Properties.Add(new DemoProperty
                {
                    Name = type.Name,
                    Value = section
                });
            }
        }

        var sb = new StringBuilder();
        WriteObject(sb, root, 0);
        return sb.ToString();
    }

    private static DemoObject? BuildObject(Type type, HashSet<Type> visited)
    {
        if (!visited.Add(type))
        {
            return null;
        }

        var obj = new DemoObject();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.MetadataToken);

        foreach (var prop in props)
        {
            var attr = prop.GetCustomAttribute<AppSettingsDemoAttribute>();
            if (attr != null)
            {
                var jsonValue = ParseToJsonValue(attr.DefaultValue);
                obj.Properties.Add(new DemoProperty
                {
                    Name = prop.Name,
                    Value = new DemoScalar(jsonValue),
                    Comment = attr.Description
                });
                continue;
            }

            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            // Handle collections (arrays, List<T>, IEnumerable<T>, etc.)
            var elementType = GetCollectionElementType(propType);
            if (elementType != null)
            {
                var itemObj = BuildObject(elementType, visited);
                if (itemObj?.Properties.Count > 0)
                {
                    obj.Properties.Add(new DemoProperty
                    {
                        Name = prop.Name,
                        Value = new DemoArray(itemObj)
                    });
                }

                continue;
            }

            // Handle nested complex types
            if (propType.IsClass && propType != typeof(string))
            {
                var nested = BuildObject(propType, visited);
                if (nested?.Properties.Count > 0)
                {
                    obj.Properties.Add(new DemoProperty
                    {
                        Name = prop.Name,
                        Value = nested
                    });
                }
            }
        }

        visited.Remove(type);
        return obj.Properties.Count > 0 ? obj : null;
    }

    private static Type? GetCollectionElementType(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(IEnumerable<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IList<>) ||
                genericDef == typeof(List<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        var enumerableInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableInterface != null)
        {
            return enumerableInterface.GetGenericArguments()[0];
        }

        return null;
    }

    private static string ParseToJsonValue(string defaultValue)
    {
        try
        {
            var node = JsonNode.Parse(defaultValue);
            return node?.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ?? "null";
        }
        catch
        {
            return JsonSerializer.Serialize(defaultValue);
        }
    }

    private static void WriteObject(StringBuilder sb, DemoObject obj, int indent)
    {
        sb.AppendLine("{");
        for (int i = 0; i < obj.Properties.Count; i++)
        {
            var prop = obj.Properties[i];
            bool hasComment = !string.IsNullOrEmpty(prop.Comment);

            // 1. 输出属性名
            sb.Append(' ', indent + 2);
            sb.Append('"').Append(prop.Name).Append("\": ");

            // 2. 如果有注释，开启包装对象 `{ "value": `
            if (hasComment)
            {
                sb.AppendLine("{");
                sb.Append(' ', indent + 4);
                sb.Append("\"value\": ");
            }

            // 计算当前值输出时的基础缩进量
            int currentIndent = hasComment ? indent + 4 : indent + 2;

            // 3. 输出属性值核心逻辑
            switch (prop.Value)
            {
                case DemoScalar scalar:
                    sb.Append(scalar.Json);
                    break;
                case DemoObject nested:
                    WriteObject(sb, nested, currentIndent);
                    break;
                case DemoArray array:
                    sb.AppendLine("[");
                    sb.Append(' ', currentIndent + 2);
                    WriteObject(sb, array.Item, currentIndent + 2);
                    sb.AppendLine();
                    sb.Append(' ', currentIndent);
                    sb.Append(']');
                    break;
            }

            // 4. 如果有注释，关闭包装对象，追加 `"comment"` 字段
            if (hasComment)
            {
                sb.AppendLine(",");
                sb.Append(' ', indent + 4);
                // 对注释内容进行基本的转义，防止特殊字符或双引号破坏 JSON 结构
                string safeComment = HttpUtility.JavaScriptStringEncode(prop.Comment);
                sb.Append("\"comment\": \"").Append(safeComment).Append("\"");
                sb.AppendLine();
                sb.Append(' ', indent + 2);
                sb.Append('}');
            }

            // 5. 统一处理同级节点间的逗号分隔符
            if (i < obj.Properties.Count - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }

        sb.Append(' ', indent);
        sb.Append('}');
    }

// 辅助转义工具方法，避免注释中带双引号或反斜杠导致合法 JSON 报错
    private static string JavaScriptStringEncode(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    private abstract class DemoValue;

    private sealed class DemoScalar(string json) : DemoValue
    {
        public string Json { get; } = json;
    }

    private sealed class DemoObject : DemoValue
    {
        public List<DemoProperty> Properties { get; } = [];
    }

    private sealed class DemoArray(DemoObject item) : DemoValue
    {
        public DemoObject Item { get; } = item;
    }

    private sealed class DemoProperty
    {
        public required string Name { get; init; }
        public string? Comment { get; init; }
        public required DemoValue Value { get; init; }
    }
}