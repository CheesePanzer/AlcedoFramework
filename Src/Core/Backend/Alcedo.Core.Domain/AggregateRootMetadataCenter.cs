using System.Reflection;
using Alcedo.Core.Abstractions;
using Alcedo.Core.Domain.Attributes;
using Alcedo.Core.Domain.Entity;

namespace Alcedo.Core.Domain;

/// <summary>
/// Central registry for Aggregate Root metadata.
/// </summary>
/// <remarks>
/// This center manages two distinct identification strategies:
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Global Unique Identifier (AggCode):</b> A solution-wide unique token 
///       (e.g., "SO", "SKU") used as the primary technical key for cross-module 
///       referencing and high-performance lookups.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Human-Readable Full Name (ModuleName.AggName):</b> A fully qualified 
///       logical descriptor (e.g., "Sales.SalesOrder") that provides business context 
///       and clarity for logging, UI display, and developer communication.
///     </description>
///   </item>
/// </list>
/// </remarks>
public sealed record AggregateRootMetadata
{
    /// <summary>
    /// Aggregate's Code, unique in module scope
    /// </summary>
    public string Code { get; init; }

    /// <summary>
    /// Aggregate's human friendly name
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// module's Code, unique in sln, which is declared by IAlcedoModule
    /// </summary>
    public string ModuleCode { get; init; }

    /// <summary>
    /// module's human friendly name, which is declared by IAlcedoModule
    /// </summary>
    public string ModuleName { get; init; }

    /// <summary>
    /// The Full Readable Name => $"{moduleName}.{name}"
    /// </summary>
    public string FullName { get; }

    /// <summary>
    /// The unique identifier in sln, represents an aggregate root type across modules => $"{moduleCode}_{code}".ToUpperInvariant()
    /// </summary>
    public string Identity { get; }

    public AggregateRootMetadata(string code, string name, string moduleCode, string moduleName)
    {
        Code = code;
        Name = name;
        ModuleCode = moduleCode;
        ModuleName = moduleName;

        FullName = $"{moduleName}.{name}";
        Identity = $"{moduleCode}_{code}".ToUpperInvariant();
    }
}

public static class AggregateRootMetadataCenter
{
    private static IReadOnlyDictionary<Type, AggregateRootMetadata> _typeCache
        = new Dictionary<Type, AggregateRootMetadata>();

    private static IReadOnlyDictionary<string, Type> _codeCache
        = new Dictionary<string, Type>();

    internal static void Initialize(IReadOnlyCollection<IAlcedoModule> modules)
    {
        var dict = new Dictionary<Type, AggregateRootMetadata>();

        var codeDict = new Dictionary<string, Type>();

        foreach (var module in modules)
        {
            var aggTypes = module.Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AggregateRoot)));

            foreach (var aggType in aggTypes)
            {
                var result = InferAggregateDescriptionOrGetFromAttribute(aggType);

                var meta = new AggregateRootMetadata(result.AggCode, result.AggName, module.ModuleCode,
                    module.ModuleName);

                dict[aggType] = meta;

                if (!codeDict.TryAdd(meta.Identity, aggType))
                {
                    throw new InvalidOperationException(
                        $"Duplicate aggregate root code: {meta.Identity} in {meta.FullName}");
                }
            }
        }

        _typeCache = dict;

        _codeCache = codeDict;
    }

    private static (string AggCode, string AggName) InferAggregateDescriptionOrGetFromAttribute(Type aggType)
    {
        var attr = aggType.GetCustomAttribute<AggregateRootDescriptionAttribute>();
        if (attr is not null)
        {
            return (attr.AggCode, attr.AggName);
        }

        string inferredName = aggType.Name;

        if (inferredName.EndsWith("AggregateRoot"))
        {
            inferredName = inferredName.Substring(0, inferredName.Length - "AggregateRoot".Length);
        }
        else if (inferredName.EndsWith("AggRoot"))
        {
            inferredName = inferredName.Substring(0, inferredName.Length - "AggRoot".Length);
        }
        else
        {
            throw new InvalidOperationException(
                $"Invalid aggregate root description: {aggType.Name}. The Name doesn't end with 'AggRoot' or 'AggregateRoot' and no AggregateRootDescriptionAttribute found on the type");
        }

        return (inferredName, inferredName);
    }

    public static IReadOnlyList<(Type, AggregateRootMetadata)> GetAllMetaData()
    {
        return _typeCache.Select(kvp => (kvp.Key, kvp.Value)).ToList();
    }

    public static Type GetAggregateTypeByIdentity(string aggCode)
    {
        if (_codeCache.TryGetValue(aggCode.ToUpperInvariant(), out var type))
        {
            return type;
        }

        throw new InvalidOperationException(
            $"AggregateRoot with {aggCode} does not exist.");
    }

    public static AggregateRootMetadata GetMetaData<TAggregate>()
        where TAggregate : AggregateRoot
        => GetMetaData(typeof(TAggregate));

    public static AggregateRootMetadata GetMetaData(Type aggregateType)
    {
        if (!aggregateType.IsAssignableTo(typeof(AggregateRoot)))
        {
            throw new InvalidOperationException(
                $"{aggregateType.Name} is not aggregateRoot");
        }

        if (_typeCache.TryGetValue(aggregateType, out var meta))
        {
            return meta;
        }

        throw new InvalidOperationException(
            $"{aggregateType.Name} is not registered or missing [AggregateRootDescription] attribute.");
    }
}

public static class AggregateRootExtensions
{
    public static AggregateRootMetadata GetMetaData(this AggregateRoot aggregate)
        => AggregateRootMetadataCenter.GetMetaData(aggregate.GetType());
}