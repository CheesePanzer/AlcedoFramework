using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Presentation;
using Alcedo.Framework.Admin.Application.AccessLog;
using Alcedo.Framework.Shared.Controller;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Admin.Controller;

[Area("admin")]
[ApiVersion("1.0")]
[Resource("log", "Manage logs under Admin")]
public sealed class LogsController : AlcedoFmApiAreaControllerBase
{
    private readonly IAccessLogApplicationService _accessLogApplicationService;

    public LogsController(IAccessLogApplicationService accessLogApplicationService)
    {
        _accessLogApplicationService = accessLogApplicationService;
    }

    [HttpGet("accesslog")]
    [Operation("ViewAccessLog")]
    public async Task<ActionResult<PagedResult<AccessLogDto>>> GetAccessLogs(
        [FromQuery] AccessLogSearchQuery query,
        [FromQuery] SortQuery sortQuery, [FromQuery] PaginationQuery paginationQuery,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _accessLogApplicationService.GetPagedDtosAsync(query, sortQuery, paginationQuery,
            cancellationToken));
    }
}
