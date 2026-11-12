using Alcedo.Frontend.Abstraction;
using Alcedo.Frontend.Abstraction.BackednInterface;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;
using Alcedo.Frontend.Identity.Clients.Role;

namespace Alcedo.Frontend.Identity.Clients.User;

[ApiClient(Area = "identity", Controller = "users", Version = 1)]
public interface IUserClient : IMasterDataController<UserDto, UserSearchQuery, UserCreateCommand, UserUpdateCommand>
{
    [HttpPatch("{id}/status:archive")]
    Task ArchiveAsync([ToRoute] Guid id, CancellationToken ct = default);

    [HttpPatch("{id}/status:lock")]
    Task LockAsync([ToRoute] Guid id, CancellationToken ct = default);

    [HttpPatch("{id}/status:unlock")]
    Task UnlockAsync([ToRoute] Guid id, CancellationToken ct = default);

    [HttpPost("{id}/password:reset")]
    Task<string> ResetPasswordAsync([ToRoute] Guid id, CancellationToken ct = default);

    [HttpPatch("{id}/user-roles:assign")]
    Task AssignRolesAsync([ToRoute] Guid id, [ToBody] IReadOnlyCollection<Guid> roleIds,
        CancellationToken ct = default);

    [HttpPatch("{id}/user-roles:delete")]
    Task DeleteRolesAsync([ToRoute] Guid id, [ToBody] IReadOnlyCollection<Guid> roleIds,
        CancellationToken ct = default);

    [HttpPatch("{id}/user-roles:clear")]
    Task ClearRolesAsync([ToRoute] Guid id, CancellationToken ct = default);

    [HttpGet("{id}/roles")]
    Task<PagedResult<RoleDto>> GetRolesAsync([ToRoute] Guid id,[ToQuery] PaginationQuery paginationQuery, CancellationToken ct = default);

    [HttpPatch("{id}/twofactor:disable")]
    Task DisableTwoFactorAsync([ToRoute] Guid id, CancellationToken ct = default);
}
