using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Presentation;
using Alcedo.Framework.Identity.Application.Dto.Log;
using Alcedo.Framework.Identity.Application.SearchQuery;
using Alcedo.Framework.Identity.Application.Service.Log;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Identity.Controller;

[Area("identity")]
[ApiVersion("1.0")]
[Resource("Log", "Identity related logs")]
public sealed class LogsController : AlcedoApiAreaControllerBase
{
    private readonly ILoginLogApplicationService _service;

    public LogsController(ILoginLogApplicationService service)
    {
        _service = service;
    }

    [HttpGet("loginlog")]
    [Operation("ViewLoginLog", "View Site's Login Log")]
    public async Task<ActionResult<PagedResult<LoginLogDto>>> GetLoginLogs([FromQuery] LoginLogSearchQuery query,
        [FromQuery] SortQuery sortQuery, [FromQuery] PaginationQuery paginationQuery,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _service.GetPagedDtosAsync(query, sortQuery, paginationQuery,
            cancellationToken));
    }
}