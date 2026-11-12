using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Shared.Application.IQueryRepository;

public interface IMasterDataQueryRepositoryBase<TEntity, TDto, TQuery> : IEntityQueryRepository<TEntity, TDto, TQuery>
    where TEntity : MasterDataAggregateRootBase
    where TDto : MasterDataDtoBase
    where TQuery : MasterDataSearchQueryBase
{
    
}