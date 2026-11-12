using System.Text;
using System.Text.Json;
using Alcedo.Core.Domain.ReferenceIntegrity;

namespace Alcedo.Core.Plugin.SmartDoc;

public static class AggregateRootReferenceRelationsDocGenerator
{
    public static string GenerateMd()
    {
        var configurations = ReferenceConfigurationCenter.Configurations;
        var sb = new StringBuilder();

        sb.AppendLine(
            "|SourceAggregateType|DependentEntityType|PrincipalAggregateType|ForeignKeyProperty|ReferenceMode|NavigationPath|");
        sb.AppendLine("|---|---|---|---|---|---|");

        foreach (var config in configurations)
        {
            string pathString = "Directly Reference";
            if (config.NavigationPath.Count > 0)
            {
                pathString = string.Join(" -> ", config.NavigationPath.Select(step =>
                    $"{step.ParentEntityType.Name}.{step.PropertyName} ({step.PropertyType.Name} {(step.IsCollection ? "[]" : "")})"));
            }

            sb.AppendLine(
                $"|{config.SourceAggregateType?.Name}|{config.DependentEntityType?.Name}|{config.PrincipalAggregateType?.Name}|{config.ForeignKeyProperty}|{config.ReferenceMode}|{pathString}|");
        }

        return sb.ToString();
    }

    public record NavigationStepDto(
        string ParentEntityType,
        string PropertyName,
        string PropertyType,
        bool IsCollection
    );

    public record ReferenceConfigurationDto(
        string SourceAggregateType,
        string DependentEntityType,
        string PrincipalAggregateType,
        string ForeignKeyProperty,
        string ReferenceMode,
        List<NavigationStepDto> NavigationPath
    );

    public static string GenerateJson()
    {
        var configurations = ReferenceConfigurationCenter.Configurations;
        var dtoLines = configurations.Select(config => new ReferenceConfigurationDto(
            SourceAggregateType: config.SourceAggregateType.FullName ?? "FullName Is Empty",
            DependentEntityType: config.DependentEntityType.FullName ?? "FullName Is Empty",
            PrincipalAggregateType: config.PrincipalAggregateType.FullName ?? "FullName Is Empty",
            ForeignKeyProperty: config.ForeignKeyProperty,
            ReferenceMode: config.ReferenceMode.ToString(),
            NavigationPath: config.NavigationPath?.Select(step => new NavigationStepDto(
                ParentEntityType: step.ParentEntityType.FullName ?? "FullName Is Empty",
                PropertyName: step.PropertyName,
                PropertyType: step.PropertyType.FullName ?? "FullName Is Empty",
                IsCollection: step.IsCollection
            )).ToList() ?? new List<NavigationStepDto>()
        )).ToList();
        return JsonSerializer.Serialize(dtoLines);
    }
}