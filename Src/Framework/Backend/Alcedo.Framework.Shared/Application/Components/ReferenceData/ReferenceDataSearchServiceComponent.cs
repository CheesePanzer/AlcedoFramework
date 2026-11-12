using Alcedo.Core.Application.Component;
using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Shared.Application.Components.ReferenceData;

public class
    ReferenceDataSearchServiceComponent<TEntity, TDto, TQuery> : AggregateRootSearchServiceComponent<TEntity, TDto,
    TQuery>
    where TEntity : ReferenceDataAggregateRootBase
    where TDto : ReferenceDataDtoBase
    where TQuery : ReferenceDataSearchQueryBase
{
    public ReferenceDataSearchServiceComponent(IEntityQueryRepository<TEntity, TDto, TQuery> repository) :
        base(repository)
    {
    }
}