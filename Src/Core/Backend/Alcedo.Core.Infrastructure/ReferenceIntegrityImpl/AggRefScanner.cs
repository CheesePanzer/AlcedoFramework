using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.ReferenceIntegrity;

namespace Alcedo.Core.Infrastructure.ReferenceIntegrityImpl;

internal class AggRefScanner : IAggRefScanner
{
    private readonly ReferenceIntegrityConfigCenter _relationRegistry;

    public AggRefScanner(
        ReferenceIntegrityConfigCenter relationRegistry)
    {
        _relationRegistry = relationRegistry;
    }

    public IReadOnlyDictionary<Type, IReadOnlyCollection<Guid>> Scan<T>(T aggregate) where T : AggregateRoot
    {
        var func = _relationRegistry.GetRefIdCollectorFunc<T>()
                   ?? throw new InvalidOperationException(
                       $"No reference configuration found for {typeof(T).Name}. " +
                       $"Ensure {typeof(T).Name} has a corresponding IEntityReferenceConfiguration implementation.");

        return func(aggregate);
    }
}