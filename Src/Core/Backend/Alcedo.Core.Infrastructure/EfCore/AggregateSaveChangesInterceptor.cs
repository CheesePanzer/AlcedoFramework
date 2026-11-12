using Alcedo.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Alcedo.Core.Infrastructure.EfCore;

internal sealed class AggregateSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<AggregateSaveChangesInterceptor> _logger;

    public AggregateSaveChangesInterceptor(ILogger<AggregateSaveChangesInterceptor> logger)
    {
        _logger = logger;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTouch(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyTouch(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void ApplyTouch(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var visitedAggs = new HashSet<EntityBase>(ReferenceEqualityComparer.Instance);

        foreach (var entry in context.ChangeTracker.Entries<AggregateRoot>())
        {
            ApplyTouchForEntryPhaseOne(context, entry, visitedAggs);
        }

        var aggIndex = context.ChangeTracker.Entries<AggregateRoot>()
            .ToDictionary(
                e => (e.Metadata.ClrType, e.Property(a => a.Id).CurrentValue),
                e => e);

        var visitedAggsTwo = new HashSet<EntityBase>(ReferenceEqualityComparer.Instance);

        foreach (var entry in context.ChangeTracker.Entries<EntityBase>()
                     .Where(e => e.Entity is not AggregateRoot && e.State == EntityState.Deleted))
        {
            ApplyTouchForEntryPhaseTwo(context, entry, aggIndex, visitedAggsTwo);
        }
    }

    /// <summary>
    /// Phase one of the touch logic. Applies touch to an aggregate root entry based on its own state,
    /// or detects modified/added descendants via navigation traversal when the root itself is unchanged.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="entry"></param>
    /// <param name="visitedAggs"></param>
    private void ApplyTouchForEntryPhaseOne(DbContext context, EntityEntry<AggregateRoot> entry,
        HashSet<EntityBase> visitedAggs)
    {
        if (!visitedAggs.Add(entry.Entity))
        {
            return;
        }

        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.TouchForCreate();
                _logger.LogDebug("TouchForCreate applied to {AggregateType}:{Id}", entry.Entity.GetType().Name,
                    entry.Entity.Id);
                break;

            case EntityState.Modified:
                entry.Entity.TouchForUpdate();
                _logger.LogDebug("TouchForUpdate applied to {AggregateType}:{Id} due to self is modified",
                    entry.Entity.GetType().Name, entry.Entity.Id);
                break;

            case EntityState.Unchanged:
                if (entry.Entity.ForceModified)
                {
                    entry.Entity.TouchForUpdate();
                    entry.Entity.ResetModified();
                    
                    //Don't set EntityState directly
                    entry.Property(nameof(AggregateRoot.UpdatedAt)).IsModified = true;
                    entry.Property(nameof(AggregateRoot.Version)).IsModified = true;
                    
                    _logger.LogDebug("TouchForUpdate applied to {AggregateType}:{Id} due to be marked as ForceModified",
                        entry.Entity.GetType().Name, entry.Entity.Id);
                    break;
                }

                var visitedEntities = new HashSet<object>(ReferenceEqualityComparer.Instance);

                if (HasModifiedDescendants(context, entry, visitedEntities))
                {
                    entry.Entity.TouchForUpdate();
                    
                    entry.Property(nameof(AggregateRoot.UpdatedAt)).IsModified = true;
                    entry.Property(nameof(AggregateRoot.Version)).IsModified = true;
                    
                    _logger.LogDebug("TouchForUpdate applied to {AggregateType}:{Id} due to has modified descendants",
                        entry.Entity.GetType().Name, entry.Entity.Id);
                }

                break;
        }
    }

    private static bool HasModifiedDescendants(DbContext context, EntityEntry entry, HashSet<object> visited)
    {
        foreach (var navigation in entry.Navigations)
        {
            if (navigation.CurrentValue is IEnumerable<EntityBase> children)
            {
                foreach (var child in children)
                {
                    var childEntry = context.Entry(child);

                    if (!visited.Add(childEntry.Entity))
                    {
                        continue;
                    }

                    if (childEntry.State != EntityState.Unchanged)
                    {
                        return true;
                    }

                    if (HasModifiedDescendants(context, childEntry, visited))
                    {
                        return true;
                    }
                }
            }
            else if (navigation.CurrentValue is EntityBase single)
            {
                var childEntry = context.Entry(single);

                if (!visited.Add(childEntry.Entity))
                {
                    continue;
                }

                if (childEntry.State != EntityState.Unchanged)
                {
                    return true;
                }

                if (HasModifiedDescendants(context, childEntry, visited))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Phase two of the touch logic. Handles deleted child entities that have been detached from
    /// their parent navigation and are therefore invisible to phase one's navigation traversal.
    /// Walks up the foreign key chain to locate the owning aggregate root and applies touch if needed.
    /// </summary>
    private void ApplyTouchForEntryPhaseTwo(
        DbContext context,
        EntityEntry entry,
        Dictionary<(Type, Guid), EntityEntry<AggregateRoot>> aggIndex,
        HashSet<EntityBase> visitedAggs)
    {
        var entityNodes = AlcedoAggregateRootGraphCenter.GetNodeIndex();

        if (!entityNodes.TryGetValue(entry.Metadata.ClrType, out var node))
        {
            return;
        }

        var current = entry;
        var currentNode = node;

        while (currentNode.Parent is not null)
        {
            var fkValue = (Guid?)current.Property(currentNode.FkPropertyName!).CurrentValue;
            if (fkValue is null)
            {
                return;
            }

            var parentType = currentNode.Parent.EntityType;

            if (typeof(AggregateRoot).IsAssignableFrom(parentType))
            {
                if (!aggIndex.TryGetValue((parentType, fkValue.Value), out var aggEntry))
                {
                    return;
                }

                if (!visitedAggs.Add(aggEntry.Entity))
                {
                    return;
                }

                if (aggEntry.State == EntityState.Unchanged)
                {
                    aggEntry.Entity.TouchForUpdate();
                    
                    aggEntry.Property(nameof(AggregateRoot.UpdatedAt)).IsModified = true;
                    aggEntry.Property(nameof(AggregateRoot.Version)).IsModified = true;
                    
                    _logger.LogDebug(
                        "TouchForUpdate applied to {AggregateType}:{Id} due to a descendents was removed from it",
                        aggEntry.Entity.GetType().Name, aggEntry.Entity.Id);
                }

                return;
            }

            current = context.ChangeTracker
                .Entries()
                .SingleOrDefault(e =>
                    e.Metadata.ClrType == parentType &&
                    Equals(e.Property("Id").CurrentValue, fkValue));

            if (current is null)
            {
                _logger.LogError("Parent entity {ParentType} not found in ChangeTracker for deleted child {ChildType}",
                    parentType.Name, entry.Entity.GetType().Name);
                throw new InvalidOperationException(
                    $"Parent entity {parentType.Name} not found in ChangeTracker.");
            }

            currentNode = currentNode.Parent;
        }
    }
}