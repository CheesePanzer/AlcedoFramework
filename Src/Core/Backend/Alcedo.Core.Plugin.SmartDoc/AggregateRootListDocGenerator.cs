using System.Text;
using System.Text.Json;
using Alcedo.Core.Domain;

namespace Alcedo.Core.Plugin.SmartDoc;

public static class AggregateRootListDocGenerator
{
    public static string GenerateMd()
    {
        var metaList = AggregateRootMetadataCenter.GetAllMetaData();
        var sb = new StringBuilder();
        sb.AppendLine("|Type|Code|Name|ModuleCode|ModuleName|FullName|Identity|");
        sb.AppendLine("|-----|-----|-----|-----|-----|-----|-----|");
        foreach (var v in metaList)
        {
            var typeName = v.Item1;
            var meta = v.Item2;
            sb.AppendLine(
                $"|{typeName}|{meta.Code}|{meta.Name}|{meta.ModuleCode}|{meta.ModuleName}|{meta.FullName}|{meta.Identity}|");
        }

        return sb.ToString();
    }

    public static string GenerateJson()
    {
        var metaList = AggregateRootMetadataCenter.GetAllMetaData();
        var newList = metaList.Select(m => new AggRootMetaInfo()
        {
            TypeName = m.Item1.ToString(),
            Code = m.Item2.Code,
            Name = m.Item2.Name,
            ModuleCode = m.Item2.ModuleCode,
            ModuleName = m.Item2.ModuleName,
            FullName = m.Item2.FullName,
            Identity = m.Item2.Identity,
        });
        return JsonSerializer.Serialize(newList);
    }
}

public record AggRootMetaInfo
{
    public required string TypeName { get; set; }

    /// <summary>
    /// Aggregate's Code, unique in module scope
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Aggregate's human friendly name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// module's Code, unique in sln, which is declared by IAlcedoModule
    /// </summary>
    public required string ModuleCode { get; set; }

    /// <summary>
    /// module's human friendly name, which is declared by IAlcedoModule
    /// </summary>
    public required string ModuleName { get; set; }

    /// <summary>
    /// The Full Readable Name => $"{moduleName}.{name}"
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// The unique identifier in sln, represents an aggregate root type across modules => $"{moduleCode}_{code}".ToUpperInvariant()
    /// </summary>
    public required string Identity { get; set; }
}