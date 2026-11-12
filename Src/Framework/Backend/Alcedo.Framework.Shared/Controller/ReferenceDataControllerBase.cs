using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Presentation;
using Alcedo.Framework.Shared.Application;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Application.Service;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Shared.Controller;

public abstract class ReferenceDataApiControllerBase<TDto, TSearch> : AlcedoFmApiAreaControllerBase
    where TDto : ReferenceDataDtoBase
    where TSearch : ReferenceDataSearchQueryBase
{
    protected readonly IReferenceDataApplicationService<TDto, TSearch> Service;

    protected ReferenceDataApiControllerBase(IReferenceDataApplicationService<TDto, TSearch> service)
    {
        Service = service;
    }

    [HttpGet]
    [Operation("PageView", "View Items' List")]
    public virtual async Task<ActionResult<PagedResult<TDto>>> GetPagedItems([FromQuery] TSearch query,
        [FromQuery] SortQuery sortQuery, [FromQuery] PaginationQuery paginationQuery,
        CancellationToken cancellationToken = default)
    {
        return Ok(await Service.GetPagedDtosAsync(query, sortQuery, paginationQuery, cancellationToken));
    }

    [HttpGet("{id}")]
    [Operation("DetailView", "View Item's Detail")]
    public virtual async Task<ActionResult<TDto>> GetItemInfo(Guid id,
        CancellationToken cancellationToken = default)
    {
        return Ok(await Service.GetDtoByIdAsync(id, cancellationToken));
    }
}