using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.IRepositoryBase;
using Alcedo.Core.Infrastructure.EfCore;

namespace Alcedo.Core.Infrastructure.RepositoryBase;

public class AggregateRootRepositoryBase<T> : EntityStoreBase<T>, IAggregateRootRepositoryBase<T>
    where T : AggregateRoot

{
    public AggregateRootRepositoryBase(AlcedoDbContext context) : base(context)
    {
    }
}