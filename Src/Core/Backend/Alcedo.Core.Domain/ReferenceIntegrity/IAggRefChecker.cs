using Alcedo.Core.Domain.Entity;

namespace Alcedo.Core.Domain.ReferenceIntegrity;

public interface IAggRefChecker
{
    /// <summary>
    /// Check agg if referenced
    /// </summary>
    Task<bool> IsReferencedAsync<T>(Guid id, CancellationToken ct = default)
        where T : AggregateRoot;

    /// <summary>
    /// Check agg if referenced
    /// </summary>
    Task<bool> IsReferencedAsync(Type aggregateType, Guid id, CancellationToken ct = default);

    /// <summary>
    /// If agg is referenced then throw an exception
    /// </summary>
    Task ThrowIfReferencedAsync<T>(Guid id, CancellationToken ct = default)
        where T : AggregateRoot;

    /// <summary>
    /// If agg is referenced then throw an exception
    /// </summary>
    Task ThrowIfReferencedAsync(Type aggregateType, Guid id, CancellationToken ct = default);
}