using Alcedo.Core.Application.Dto;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Domain.Entity;

namespace Alcedo.Core.Application.IQueryRepository;

public interface IEntityQueryRepository<TEntity, TDto, in TQuery>
    where TEntity : EntityBase
    where TDto : EntityDtoBase
    where TQuery : EntitySearchQueryBase
{
    /// <summary>
    /// Common pagination method, notably it has a transaction of level repeatable read
    /// </summary>
    /// <param name="query"></param>
    /// <param name="pagiQuery"></param>
    /// <param name="sortQuery"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<(IReadOnlyList<TDto> Items, int TotalCount)> GetPagedAsync(
        TQuery query,
        PaginationQuery pagiQuery,
        SortQuery sortQuery,
        CancellationToken ct = default);

    Task<IReadOnlyList<TDto>> GetListAsync(
        TQuery query,
        SortQuery sortQuery,
        CancellationToken ct = default);

    Task<TDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<TDto>> GetListByIdsAsync(IReadOnlyCollection<Guid> id, CancellationToken ct = default);
}