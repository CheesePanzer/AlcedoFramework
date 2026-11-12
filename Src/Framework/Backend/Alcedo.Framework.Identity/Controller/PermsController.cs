using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Presentation;
using Alcedo.Framework.Identity.Application.Dto;
using Alcedo.Framework.Identity.Application.SearchQuery;
using Alcedo.Framework.Identity.Application.Service.Perm;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Identity.Controller;

[Area("identity")]
[ApiVersion("1.0")]
[Resource("Permissions", "Manage Site's Permissions")]
public sealed class PermsController : AlcedoApiAreaControllerBase
{
    private readonly IPermApplicationService _service;

    public PermsController(IPermApplicationService service)
    {
        _service = service;
    }

    [HttpGet]
    [Operation("PageView", "View Items' List")]
    public ActionResult<PagedResult<PermDto>> GetPagedItems([FromQuery] PermSearchQuery query,
        [FromQuery] PaginationQuery paginationQuery)
    {
        return Ok(_service.GetPagedDtosAsync(query, paginationQuery));
    }
}