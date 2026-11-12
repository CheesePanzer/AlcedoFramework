using Alcedo.Core.Application.Dto;
using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Domain.Entity;

namespace Alcedo.Core.Application.Component;

public class AggregateRootSearchServiceComponent<TEntity, TDto, TSearchQuery>
    : EntitySearchServiceComponent<TEntity, TDto, TSearchQuery>
    where TEntity : AggregateRoot
    where TDto : AggregateRootDtoBase
    where TSearchQuery : AggregateRootSearchQueryBase
{
    public AggregateRootSearchServiceComponent(
        IEntityQueryRepository<TEntity, TDto, TSearchQuery> queryRepository)
        : base(queryRepository)
    {
    }
}