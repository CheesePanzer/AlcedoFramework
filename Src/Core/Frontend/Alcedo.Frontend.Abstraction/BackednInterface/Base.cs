using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;

namespace Alcedo.Frontend.Abstraction.BackednInterface;

public interface IPagedQueryController<TDto, in TQuery>
{
    [HttpGet]
    Task<PagedResult<TDto>> GetListAsync(TQuery query, SortQuery sortQuery, PaginationQuery paginationQuery);

    [HttpGet("{id}")]
    Task<TDto> GetAsync([ToRoute] Guid id);
}