using System.Reflection;

namespace Alcedo.Frontend.Blazor.Components.Auto;

public static class AutoComponentOrderedProperties
{
    public static IEnumerable<PropertyInfo> Get(Type type)
    {
        var hierarchy = new List<Type>();
        var current = type;
        while (current != null && current != typeof(object))
        {
            hierarchy.Insert(0, current);
            current = current.BaseType;
        }
        return hierarchy
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
    }
}