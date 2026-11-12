using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Presentation;
using Alcedo.Framework.Identity.Application.Command;
using Alcedo.Framework.Identity.Application.Dto.Role;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Application.SearchQuery;
using Alcedo.Framework.Identity.Application.Service.User;
using Alcedo.Framework.Shared.Controller;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Identity.Controller;

[Area("identity")]
[ApiVersion("1.0")]
[Resource("Users", "Manage Users")]
public sealed class
    UsersController : MasterDataControllerBase<UserDto, UserSearchQuery, UserCreateCommand, UserUpdateCommand>
{
    private readonly IUserApplicationService _service;

    public UsersController(IUserApplicationService service) : base(service)
    {
        _service = service;
    }

    [HttpPost("{id}/password:reset")]
    [Operation("ResetPassword", "Reset a User's Password")]
    public async Task<ActionResult<string>> ResetPassword(Guid id, CancellationToken cancellationToken = default)
    {
        return Ok(await _service.ResetPasswordAsync(id, CurrentUser, cancellationToken));
    }

    [HttpPatch("{id}/status:lock")]
    [Operation("Lock", "Lock A User")]
    public async Task<IActionResult> LockUser
        (Guid id, CancellationToken cancellationToken = default)
    {
        await _service.LockUserAsync(id, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpPatch("{id}/status:unlock")]
    [Operation("Unlock", "Unlock A User")]
    public async Task<IActionResult> UnLockUser(Guid id, CancellationToken cancellationToken = default)
    {
        await _service.UnlockUserAsync(id, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpPatch("{id}/user-roles:assign")]
    [Operation("AssignRoles", "Batch Assign Roles to User")]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        await _service.BatchAssignRolesToUserAsync(id, roleIds, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpPatch("{id}/user-roles:delete")]
    [Operation("DeleteRoles", "Batch Delete Roles Of A User")]
    public async Task<IActionResult> DeleteRole(Guid id, [FromBody] IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        await _service.BatchDeleteRolesForUserAsync(id, roleIds, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpPatch("{id}/user-roles:clear")]
    [Operation("ClearRoles", "Clear Roles For A User")]
    public async Task<IActionResult> ClearRoles(Guid id, CancellationToken cancellationToken = default)
    {
        await _service.ClearRolesForUserAsync(id, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpPatch("{id}/status:archive")]
    [Operation("Archive", "Archive A User")]
    public async Task<IActionResult> ArchiveUser(Guid id, CancellationToken cancellationToken = default)
    {
        await _service.ArchiveByIdAsync(id, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpPatch("{id}/twofactor:disable")]
    [Operation("DisableTotp", "Disable Totp Login For a User")]
    public async Task<IActionResult> DisableTwoFactor(Guid id, CancellationToken cancellationToken = default)
    {
        await _service.DisableTwoFactorAuthenticationAsync(id, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpGet("{id}/roles")]
    [Operation("ViewRoles", "View Roles Of a User")]
    public async Task<ActionResult<PagedResult<RoleDto>>> GetRoleDtosByUserId([FromRoute] Guid id, [FromQuery] PaginationQuery paginationQuery,
        CancellationToken cancellationToken = default)
    {
        var roles = await _service.GetRoleDtosByUserIdAsync(id, paginationQuery, cancellationToken);
        return Ok(roles);
    }
}
