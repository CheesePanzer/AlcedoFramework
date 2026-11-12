using Alcedo.Core.Application.AppException;
using Alcedo.Core.Application.Dto;
using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Domain.Entity;

namespace Alcedo.Core.Application.Component;

public class EntitySearchServiceComponent<TEntity, TDto, TQuery>
    where TEntity : EntityBase
    where TDto : EntityDtoBase
    where TQuery : EntitySearchQueryBase
{
    protected readonly IEntityQueryRepository<TEntity, TDto, TQuery> QueryRepository;

    public EntitySearchServiceComponent(IEntityQueryRepository<TEntity, TDto, TQuery> queryRepository)
    {
        QueryRepository = queryRepository;
    }

    public async Task<PagedResult<TDto>> GetPagedDtosAsync(TQuery query, SortQuery sortQuery, PaginationQuery pagiQuery,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await QueryRepository.GetPagedAsync(
            query, pagiQuery, sortQuery, ct);

        return new PagedResult<TDto>
        {
            Items = items,
            PageNo = pagiQuery.PageNo,
            PageSize = pagiQuery.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<TDto> GetDtoByIdAsync(Guid id, CancellationToken ct = default)
        => await QueryRepository.GetByIdAsync(id, ct) ?? throw new AppErrQueryResultNotFound<TDto>(id);
}