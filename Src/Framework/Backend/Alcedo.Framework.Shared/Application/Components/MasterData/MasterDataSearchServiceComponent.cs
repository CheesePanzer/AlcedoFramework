using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Framework.Shared.Application.Components.ReferenceData;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Shared.Application.Components.MasterData;

public class
    MasterDataSearchServiceComponent<TEntity, TDto, TQuery> : ReferenceDataSearchServiceComponent<TEntity, TDto, TQuery>
    where TEntity : MasterDataAggregateRootBase
    where TDto : MasterDataDtoBase
    where TQuery : MasterDataSearchQueryBase
{
    public MasterDataSearchServiceComponent(
        IEntityQueryRepository<TEntity, TDto, TQuery> repository) : base(repository)
    {
    }
}