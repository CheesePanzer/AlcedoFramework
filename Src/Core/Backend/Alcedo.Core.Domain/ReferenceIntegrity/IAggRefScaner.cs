using Alcedo.Core.Domain.Entity;

namespace Alcedo.Core.Domain.ReferenceIntegrity;

public interface IAggRefScanner
{
    /// <summary>
    /// Scan aggregate root and return a dictionary of the types and ids of the referenced aggregateroot
    /// </summary>
    IReadOnlyDictionary<Type, IReadOnlyCollection<Guid>> Scan<T>(T aggregate)
        where T : AggregateRoot;
}