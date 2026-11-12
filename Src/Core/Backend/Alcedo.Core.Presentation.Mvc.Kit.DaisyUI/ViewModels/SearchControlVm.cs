using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Alcedo.Core.Application.SearchQuery;

namespace Alcedo.Core.Presentation.Mvc.Kit.DaisyUI.ViewModels;

[AttributeUsage(AttributeTargets.Property)]
public sealed class IgnoreRenderSearchFieldAttribute : Attribute;

public sealed class SearchControlVm
{
    public List<SearchFieldMetadata> SearchFields { get; init; } = new();
    public string TargetAction { get; init; } = "";

    public static SearchControlVm Parse<TQuery>(TQuery query, string targetAction,
        IReadOnlyCollection<string> ignoredProperties)
        where TQuery : EntitySearchQueryBase
    {
        var type = query.GetType();
        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<IgnoreRenderSearchFieldAttribute>() is null)
            .Where(p => !ignoredProperties.Contains(p.Name));

        var searchFields = properties
            .Select(p => new SearchFieldMetadata(
                Name: p.Name,
                DisplayName: p.GetCustomAttribute<DisplayAttribute>()?.Name ?? p.Name,
                Type: p.PropertyType,
                Value: p.GetValue(query)
            ))
            .ToList();

        return new SearchControlVm
        {
            SearchFields = searchFields,
            TargetAction = targetAction
        };
    }
}

public record SearchFieldMetadata(
    string Name,
    string DisplayName,
    Type Type,
    object? Value
);
