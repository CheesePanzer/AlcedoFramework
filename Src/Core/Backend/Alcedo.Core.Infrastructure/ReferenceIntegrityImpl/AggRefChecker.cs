using Alcedo.Core.Domain;
using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Core.Infrastructure.EfCore;
using Microsoft.Extensions.Logging;

namespace Alcedo.Core.Infrastructure.ReferenceIntegrityImpl;

internal class AggRefChecker : IAggRefChecker
{
    private readonly AlcedoDbContext _db;
    private readonly ReferenceIntegrityConfigCenter _relationRegistry;
    private readonly ILogger<AggRefChecker> _logger;

    public AggRefChecker(
        AlcedoDbContext db,
        ReferenceIntegrityConfigCenter relationRegistry,
        ILogger<AggRefChecker> logger)
    {
        _db = db;
        _relationRegistry = relationRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Checks if the entity is referenced and returns a boolean. 
    /// Note: For strong consistency in concurrent scenarios, lock the entity being checked before calling.
    /// </summary>
    public async Task<bool> IsReferencedAsync<T>(Guid id, CancellationToken ct = default) where T : AggregateRoot
    {
        var validators = _relationRegistry.GetRefCheckFuncs<T>();

        if (validators.Count == 0)
        {
            return false;
        }

        foreach (var checkFunc in validators.Values)
        {
            if (await checkFunc.Invoke(_db, id, ct))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the entity is referenced and throws an exception if it is. 
    /// Note: For strong consistency in concurrent scenarios, lock the entity being checked before calling.
    /// </summary>
    /// <exception cref="BErrorAggIsReferenced"></exception>
    public async Task ThrowIfReferencedAsync<T>(Guid id, CancellationToken ct = default) where T : AggregateRoot
    {
        var validators = _relationRegistry.GetRefCheckFuncs<T>();

        if (validators.Count == 0)
        {
            return;
        }

        foreach (var (refType, checkFunc) in validators)
        {
            if (await checkFunc.Invoke(_db, id, ct))
            {
                _logger.LogDebug(
                    "Aggregate {AggType}:{Id} is referenced by {RefType}",
                    typeof(T).Name, id, refType.Name);

                throw new DomErrAggReferenced(
                    AggregateRootMetadataCenter.GetMetaData<T>().Name,
                    AggregateRootMetadataCenter.GetMetaData(refType).Name);
            }
        }
    }

    public async Task<bool> IsReferencedAsync(Type aggregateType, Guid id, CancellationToken ct = default)
    {
        if (!aggregateType.IsAssignableTo(typeof(AggregateRoot)))
        {
            throw new ArgumentException($"{aggregateType.Name} is not a subclass of AggregateRoot");
        }

        var validators = _relationRegistry.GetRefCheckFuncs(aggregateType);

        if (validators.Count == 0)
        {
            return false;
        }

        foreach (var checkFunc in validators.Values)
        {
            if (await checkFunc.Invoke(_db, id, ct))
            {
                return true;
            }
        }

        return false;
    }

    public async Task ThrowIfReferencedAsync(Type aggregateType, Guid id, CancellationToken ct = default)
    {
        if (!aggregateType.IsAssignableTo(typeof(AggregateRoot)))
        {
            throw new ArgumentException($"{aggregateType.Name} is not a subclass of AggregateRoot");
        }

        var validators = _relationRegistry.GetRefCheckFuncs(aggregateType);

        if (validators.Count == 0)
        {
            return;
        }

        foreach (var (refType, checkFunc) in validators)
        {
            if (await checkFunc.Invoke(_db, id, ct))
            {
                _logger.LogWarning(
                    "Aggregate {AggType}:{Id} is referenced by {RefType}",
                    aggregateType.Name, id, refType.Name);

                throw new DomErrAggReferenced(
                    aggregateType.Name,
                    refType.Name);
            }
        }
    }
}