using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Presentation;
using Alcedo.Framework.Identity.Application.Command;
using Alcedo.Framework.Identity.Application.Dto;
using Alcedo.Framework.Identity.Application.Dto.Role;
using Alcedo.Framework.Identity.Application.SearchQuery;
using Alcedo.Framework.Identity.Application.Service.Role;
using Alcedo.Framework.Shared.Controller;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Identity.Controller;

[Area("identity")]
[ApiVersion("1.0")]
[Resource("Roles", "Manage RBAC Rule's Role")]
public sealed class
    RolesController : MasterDataControllerBase<RoleDto, RoleSearchQuery, RoleCreateCommand, RoleUpdateCommand>
{
    private readonly IRoleApplicationService _service;

    public RolesController(
        IRoleApplicationService service) :
        base(service)
    {
        _service = service;
    }

    [HttpPatch("{id}/role-perms:assign")]
    [Operation("AssignPermissions", "Batch Assign Permissions To Role")]
    public async Task<IActionResult> AssignPerms(Guid id, [FromBody] IReadOnlyCollection<string> permKeys,
        CancellationToken cancellationToken = default)
    {
        await _service.BatchAssignPermsToRoleAsync(id, permKeys, CurrentUser.Id, cancellationToken);
        return Ok();
    }

    [HttpPatch("{id}/role-perms:delete")]
    [Operation("DeletePermissions", "Batch Delete Permissions Of Role")]
    public async Task<IActionResult> DeletePerms(Guid id, [FromBody] IReadOnlyCollection<string> permKeys,
        CancellationToken cancellationToken = default)
    {
        await _service.BatchDeletePermsForRoleAsync(id, permKeys, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpPatch("{id}/role-perms:clear")]
    [Operation("ClearPermissions", "Batch Clear Permissions Of Role")]
    public async Task<IActionResult> ClearPerms(Guid id, CancellationToken cancellationToken = default)
    {
        await _service.ClearPermsForRoleAsync(id, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpGet("{id}/perms")]
    [Operation("ViewPermissions", "View Permissions Of Role")]
    public async Task<ActionResult<PagedResult<PermDto>>> GetPermsByRoleId(Guid id,
        [FromQuery] PaginationQuery paginationQuery,
        CancellationToken cancellationToken = default)
    {
        var roles = await _service.GetPermsByRoleIdAsync(id, paginationQuery, cancellationToken);
        return Ok(roles);
    }

    [HttpPut("{id}/cache:refresh")]
    [Operation("RefreshRoleCache", "Refresh Redis Cache For Role")]
    public async Task<IActionResult> RefreshRoleCache(Guid id, CancellationToken cancellationToken = default)
    {
        await _service.RefreshRoleCacheByIdAsync(id, cancellationToken);
        return Ok();
    }
}