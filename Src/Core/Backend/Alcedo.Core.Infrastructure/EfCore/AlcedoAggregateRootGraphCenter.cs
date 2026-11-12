using Alcedo.Core.Domain.Entity;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Core.Infrastructure.EfCore;

internal sealed record AggregateGraph
{
    public Type AggregateRootType { get; init; } = null!;
    public EntityNode Root { get; init; } = null!;
}

internal sealed record EntityNode
{
    public Type EntityType { get; init; } = null!;
    public EntityNode? Parent { get; init; }
    public string? FkPropertyName { get; init; }
    public string? NavigationName { get; init; }
    public List<EntityNode> Children { get; init; } = new();
}

internal static class AlcedoAggregateRootGraphCenter
{
    private static IReadOnlyDictionary<Type, AggregateGraph> _graphs =
        new Dictionary<Type, AggregateGraph>();

    private static IReadOnlyDictionary<Type, EntityNode> _entityNodeIndex =
        new Dictionary<Type, EntityNode>();

    internal static void Initialize(AlcedoDbContext context)
    {
        var graphs = new Dictionary<Type, AggregateGraph>();

        foreach (var entityType in context.Model.GetEntityTypes()
                     .Where(e => typeof(AggregateRoot).IsAssignableFrom(e.ClrType)))
        {
            var rootNode = new EntityNode
            {
                EntityType = entityType.ClrType,
                FkPropertyName = null,
                NavigationName = null,
                Parent = null
            };

            var currentPath = new HashSet<Type> { entityType.ClrType };
            TraverseDescendants(entityType, rootNode, currentPath);

            graphs[entityType.ClrType] = new AggregateGraph
            {
                AggregateRootType = entityType.ClrType,
                Root = rootNode
            };
        }

        _graphs = graphs;

        var entityNodeIndex = new Dictionary<Type, EntityNode>();
        foreach (var graph in graphs.Values)
        {
            FlattenNodes(graph.Root, entityNodeIndex);
        }

        _entityNodeIndex = entityNodeIndex;
    }

    private static void FlattenNodes(EntityNode node, Dictionary<Type, EntityNode> index)
    {
        foreach (var child in node.Children)
        {
            if (index.ContainsKey(child.EntityType))
            {
                var existing = index[child.EntityType];
                var existingAggRoot = GetAggregateRootNode(existing);
                var currentAggRoot = GetAggregateRootNode(node);

                if (existingAggRoot.EntityType != currentAggRoot.EntityType)
                {
                    throw new InvalidOperationException(
                        $"Entity '{child.EntityType.Name}' is defined as a child in multiple aggregates: " +
                        $"'{existingAggRoot.EntityType.Name}' and '{currentAggRoot.EntityType.Name}'. " +
                        $"An entity can only belong to one aggregate.");
                }

                continue;
            }

            index[child.EntityType] = child;
            FlattenNodes(child, index);
        }
    }

    private static EntityNode GetAggregateRootNode(EntityNode node)
    {
        var current = node;
        while (current.Parent is not null)
        {
            current = current.Parent;
        }

        return current;
    }

    private static void TraverseDescendants(
        IEntityType entityType,
        EntityNode parentNode,
        HashSet<Type> currentPath)
    {
        foreach (var navigation in entityType.GetNavigations())
        {
            var targetType = navigation.TargetEntityType;

            if (!typeof(EntityBase).IsAssignableFrom(targetType.ClrType))
            {
                continue;
            }

            if (typeof(AggregateRoot).IsAssignableFrom(targetType.ClrType))
            {
                continue;
            }

            if (currentPath.Contains(targetType.ClrType))
            {
                throw new InvalidOperationException(
                    $"Cycle detected in aggregate '{entityType.ClrType.Name}': " +
                    $"'{targetType.ClrType.Name}' appears more than once in the same path.");
            }

            if (parentNode.Children.Any(c => c.NavigationName == navigation.Name))
            {
                continue;
            }
            
            var fk = navigation.ForeignKey;

            if (fk is null)
            {
                continue;
            }

            var childNode = new EntityNode
            {
                EntityType = targetType.ClrType,
                FkPropertyName = fk.Properties[0].Name,
                NavigationName = navigation.Name,
                Parent = parentNode
            };

            parentNode.Children.Add(childNode);
            currentPath.Add(targetType.ClrType);
            TraverseDescendants(targetType, childNode, currentPath);
            currentPath.Remove(targetType.ClrType);
        }
    }

    internal static AggregateGraph GetGraph(Type aggType)
        => _graphs[aggType];

    internal static IReadOnlyDictionary<Type, EntityNode> GetNodeIndex() => _entityNodeIndex;
}

public static class AlcedoAggregateRootGraphCenterExtensions
{
    public static IApplicationBuilder UseAlcedoAggregateRootGraphCenter(this IApplicationBuilder builder)
    {
        using var scope = builder.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AlcedoDbContext>();
        AlcedoAggregateRootGraphCenter.Initialize(dbContext);
        return builder;
    }
}