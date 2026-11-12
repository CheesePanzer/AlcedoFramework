using Alcedo.Core.Application;
using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Framework.Identity.Application.Dto;
using Alcedo.Framework.Identity.Application.Dto.Role;
using Alcedo.Framework.Identity.Application.SearchQuery;
using Alcedo.Framework.Identity.Domain.Entity;

namespace Alcedo.Framework.Identity.Application.IQueryRepository;

public interface IRoleQueryRepository : IEntityQueryRepository<RoleAgg, RoleDto, RoleSearchQuery>
{
    Task<PagedResult<PermDto>> GetPermsByRoleIdAsync(
        Guid roleId,
        PaginationQuery pagiQuery,
        CancellationToken ct = default);
}