using Alcedo.Frontend.Abstraction;
using Alcedo.Frontend.Abstraction.BackednInterface;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;
using Alcedo.Frontend.Identity.Clients.Perm;

namespace Alcedo.Frontend.Identity.Clients.Role;

[ApiClient(Area = "identity", Controller = "roles", Version = 1)]
public interface IRoleClient : IMasterDataController<RoleDto, RoleSearchQuery, RoleCreateCommand, RoleUpdateCommand>
{
    [HttpPatch("{id}/role-perms:assign")]
    Task AssignPermsAsync([ToRoute] Guid id, [ToBody] IReadOnlyCollection<string> permKeys,
        CancellationToken ct = default);

    [HttpPatch("{id}/role-perms:delete")]
    Task DeletePermsAsync([ToRoute] Guid id, [ToBody] IReadOnlyCollection<string> permKeys,
        CancellationToken ct = default);

    [HttpPatch("{id}/role-perms:clear")]
    Task ClearPermsAsync([ToRoute] Guid id, CancellationToken ct = default);

    [HttpGet("{id}/perms")]
    Task<PagedResult<PermDto>> GetPermsByRoleIdAsync([ToRoute] Guid id, [ToQuery] PaginationQuery paginationQuery,
        CancellationToken ct = default);

    [HttpPut("{id}/cache:refresh")]
    Task RefreshRoleCacheAsync([ToRoute] Guid id, CancellationToken ct = default);
}