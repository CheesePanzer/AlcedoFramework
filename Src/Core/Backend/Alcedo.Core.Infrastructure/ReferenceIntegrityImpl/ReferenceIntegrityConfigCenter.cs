using System.Collections.ObjectModel;
using System.Reflection;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Infrastructure.EfCore;

namespace Alcedo.Core.Infrastructure.ReferenceIntegrityImpl;

internal class ReferenceIntegrityConfigCenter
{
    private readonly ReferenceMetadataStore _metadataStore;

    public ReferenceIntegrityConfigCenter()
    {
        _metadataStore = ReferenceMetadataFactory.Create();
    }

    /// <summary>
    /// Returns reference check functions for the given aggregate root type,
    /// keyed by the referencing aggregate root type.
    /// </summary>
    public IReadOnlyDictionary<Type, Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>>> GetRefCheckFuncs<T>()
        where T : AggregateRoot
    {
        return GetRefCheckFuncs(typeof(T));
    }

    /// <summary>
    /// Returns reference check functions for the given aggregate root type,
    /// keyed by the referencing aggregate root type.
    /// </summary>
    public IReadOnlyDictionary<Type, Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>>>
        GetRefCheckFuncs(Type aggType)
    {
        if (!typeof(AggregateRoot).IsAssignableFrom(aggType))
        {
            throw new ArgumentException("Must be AggregateRoot");
        }

        return _metadataStore.CheckFuncs.TryGetValue(aggType, out var references)
            ? references
            : new ReadOnlyDictionary<Type, Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>>>(
                new Dictionary<Type, Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>>>());
    }

    public Func<AggregateRoot, IReadOnlyDictionary<Type, IReadOnlyCollection<Guid>>>? GetRefIdCollectorFunc<T>()
        where T : AggregateRoot
    {
        return GetRefIdCollectorFunc(typeof(T));
    }

    public Func<AggregateRoot, IReadOnlyDictionary<Type, IReadOnlyCollection<Guid>>>? GetRefIdCollectorFunc(
        Type aggType)
    {
        if (!typeof(AggregateRoot).IsAssignableFrom(aggType))
        {
            throw new ArgumentException("Must be AggregateRoot");
        }

        return _metadataStore.AggregateCollectors.TryGetValue(aggType, out var references)
            ? references
            : null;
    }
}