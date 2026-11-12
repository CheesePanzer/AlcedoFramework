using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;

namespace Alcedo.Framework.Shared.Application.Service;

public interface IReferenceDataApplicationService<TDto, in TSearch>
    where TDto : ReferenceDataDtoBase
    where TSearch : ReferenceDataSearchQueryBase
{
    Task<PagedResult<TDto>> GetPagedDtosAsync(TSearch query, SortQuery sortQuery, PaginationQuery pagiQuery,
        CancellationToken ct = default);

    Task<TDto> GetDtoByIdAsync(Guid id, CancellationToken ct = default);
}