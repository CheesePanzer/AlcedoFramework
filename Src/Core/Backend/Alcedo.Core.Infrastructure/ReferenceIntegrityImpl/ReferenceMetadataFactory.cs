using System.Reflection;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Core.Infrastructure.EfCore;

namespace Alcedo.Core.Infrastructure.ReferenceIntegrityImpl;

internal record NavigationMetadata(
    Func<object, object?> Getter,
    Type TargetType,
    bool IsCollection,
    string PropertyName
);

internal class ReferenceMetadataStore
{
    public IReadOnlyDictionary<Type, Dictionary<Type, Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>>>>
        CheckFuncs { get; init; } =
        new Dictionary<Type, Dictionary<Type, Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>>>>();

    public IReadOnlyDictionary<Type, Func<AggregateRoot, Dictionary<Type, IReadOnlyCollection<Guid>>>>
        AggregateCollectors { get; init; } =
        new Dictionary<Type, Func<AggregateRoot, Dictionary<Type, IReadOnlyCollection<Guid>>>>();
}

internal static class ReferenceMetadataFactory
{
    public static ReferenceMetadataStore Create()
    {
        var relations = ReferenceConfigurationCenter.Configurations;

        var checkFuncs = InitializeCheckFuncs(relations);

        var idExtractors = InitializeIdExtractors(relations);

        var navExtractors = InitializeNavigations(relations);

        var aggCollectors = InitializeAggRefCollector(relations, idExtractors, navExtractors);

        return new ReferenceMetadataStore()
        {
            CheckFuncs = checkFuncs,
            AggregateCollectors = aggCollectors
        };
    }

    /// <summary>
    /// Flattens and groups configurations to compile efficient SQL-check delegates.
    /// </summary>
    private static IReadOnlyDictionary<Type,
            Dictionary<Type, Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>>>>
        InitializeCheckFuncs(IReadOnlyCollection<ReferenceConfiguration> relations)
    {
        // 1. Group by Principal (Referenced) entity, then group by Referencing Aggregate Root internally
        var groupedByPrincipal = relations
            .Where(r => r.ReferenceMode == ReferenceMode.Strict)
            .GroupBy(r => r.PrincipalAggregateType);

        var result =
            new Dictionary<Type, Dictionary<Type, Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>>>>();

        foreach (var principalGroup in groupedByPrincipal)
        {
            var principalType = principalGroup.Key;

            // Get or create the validation dictionary for the referenced entity
            if (!result.TryGetValue(principalType, out var validatorDict))
            {
                validatorDict = new Dictionary<Type, Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>>>();
                result[principalType] = validatorDict;
            }

            // 2. Under the current referenced entity, group by Referencing Aggregate Root to handle 
            // cases where a single referrer points to the same aggregate via multiple properties (e.g., CreatorId and UpdaterId both point to userAgg).
            var rootGroups = principalGroup.GroupBy(r => r.SourceAggregateType);

            foreach (var rootGroup in rootGroups)
            {
                var rootType = rootGroup.Key;

                // Group by entity type to handle the case where multiple entities 
                // under the same aggregate root all reference the same principal.
                var entityGroups = rootGroup.GroupBy(r => r.DependentEntityType);

                var perEntityFuncs = entityGroups.Select(entityGroup =>
                {
                    var entityType = entityGroup.Key;
                    var propertyNames = entityGroup.Select(g => g.ForeignKeyProperty).Distinct().ToList();
                    //For a single dependent entity, Any(prop1 == id || prop2 ==id || ...)
                    return ReferenceIntegrityCompiler.BuildCheckReferenceFunc(entityType, propertyNames);
                }).ToList();

                // Merge: (ctx, id, ct) => entity1.Any(...) || entity2.Any(...
                validatorDict[rootType] = ReferenceIntegrityCompiler.MergeCheckFuncs(perEntityFuncs);
            }
        }

        return result;
    }

    private static Dictionary<Type, Action<object, Dictionary<Type, HashSet<Guid>>>> InitializeIdExtractors(
        IReadOnlyCollection<ReferenceConfiguration> relations)
    {
        var groups = relations.GroupBy(r => r.DependentEntityType);

        var result = new Dictionary<Type, Action<object, Dictionary<Type, HashSet<Guid>>>>();

        foreach (var group in groups)
        {
            var entityType = group.Key;
            var typeConfigs = group.ToList();

            result[entityType] = ReferenceIntegrityCompiler.CompileIdExtractor(entityType, typeConfigs);
        }

        return result;
    }

    private static Dictionary<Type, List<NavigationMetadata>> InitializeNavigations(
        IReadOnlyCollection<ReferenceConfiguration> relations)
    {
        var allSteps = relations.SelectMany(c => c.NavigationPath);

        var groups = allSteps.GroupBy(s => s.ParentEntityType);

        var result = new Dictionary<Type, List<NavigationMetadata>>();

        foreach (var group in groups)
        {
            var parentType = group.Key;
            var navList = new List<NavigationMetadata>();

            foreach (var step in group.DistinctBy(s => s.PropertyName))
            {
                navList.Add(new NavigationMetadata(
                    Getter: ReferenceIntegrityCompiler.CompileNavGetter(parentType, step.PropertyName),
                    TargetType: step.PropertyType,
                    IsCollection: step.IsCollection,
                    PropertyName: step.PropertyName
                ));
            }

            result[parentType] = navList;
        }

        return result;
    }

    /// <summary>
    /// Generates a collector for each aggregate type to scan instances for referenced IDs recursively.
    /// </summary>
    private static Dictionary<Type, Func<AggregateRoot, Dictionary<Type, IReadOnlyCollection<Guid>>>>
        InitializeAggRefCollector(IReadOnlyCollection<ReferenceConfiguration> relations,
            Dictionary<Type, Action<object, Dictionary<Type, HashSet<Guid>>>> idExtractorCache,
            Dictionary<Type, List<NavigationMetadata>> navCache)
    {
        var aggGroups = relations
            .Where(r => r.ReferenceMode != ReferenceMode.IgnoreCheck)
            .GroupBy(r => r.SourceAggregateType);

        var result = new Dictionary<Type, Func<AggregateRoot, Dictionary<Type, IReadOnlyCollection<Guid>>>>();

        foreach (var group in aggGroups)
        {
            result[group.Key] = BuildCollector(group.Key, idExtractorCache, navCache);
        }

        return result;
    }

    /// <summary>
    /// Builds a collector to extract IDs from an aggregate instance basing on dfs.
    /// </summary>
    private static Func<AggregateRoot, Dictionary<Type, IReadOnlyCollection<Guid>>> BuildCollector(
        Type rootType,
        Dictionary<Type, Action<object, Dictionary<Type, HashSet<Guid>>>> idExtractorCache,
        Dictionary<Type, List<NavigationMetadata>> navCache)
    {
        return (rootInstance) =>
        {
            var result = new Dictionary<Type, HashSet<Guid>>();
            var visited = new HashSet<object>();
            var stack = new Stack<(object Instance, Type RuntimeType)>();

            stack.Push((rootInstance, rootType));

            while (stack.Count > 0)
            {
                var (obj, currentType) = stack.Pop();

                if (obj is null || !visited.Add(obj))
                {
                    continue;
                }

                if (idExtractorCache.TryGetValue(currentType, out var extractor))
                {
                    extractor(obj, result);
                }

                // Push navigation instances into stack
                if (navCache.TryGetValue(currentType, out var navList))
                {
                    foreach (var nav in navList)
                    {
                        var val = nav.Getter(obj);
                        if (val is null)
                        {
                            continue;
                        }

                        if (nav.IsCollection)
                        {
                            foreach (var item in (System.Collections.IEnumerable)val)
                            {
                                if (item is not null)
                                {
                                    stack.Push((item, nav.TargetType));
                                }
                            }
                        }
                        else
                        {
                            stack.Push((val, nav.TargetType));
                        }
                    }
                }
            }

            return result.ToDictionary(kv => kv.Key, kv => (IReadOnlyCollection<Guid>)kv.Value);
        };
    }
}