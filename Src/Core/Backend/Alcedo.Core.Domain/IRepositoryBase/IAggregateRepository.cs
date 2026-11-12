using Alcedo.Core.Domain.Entity;

namespace Alcedo.Core.Domain.IRepositoryBase;

public interface IAggregateRootRepositoryBase<T> : IEntityStoreBase<T>
    where T : AggregateRoot;