using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Framework.Identity.Application.Dto.Role;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Application.SearchQuery;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Shared.Application.Dto;

namespace Alcedo.Framework.Identity.Application.IQueryRepository;

public interface IUserQueryRepository : IEntityQueryRepository<UserAgg, UserDto, UserSearchQuery>
{
    Task<(int Count,IReadOnlyList<RoleDto> Items)> GetRoleDtosByUserIdAsync(Guid userId, PaginationQuery paginationQuery, CancellationToken ct = default);

    Task<OperatorDto?> GetEnabledUnlockedByIdAsync(Guid userId, CancellationToken ct = default);

    Task<OperatorDtoWithRoles?> GetEnabledUnlockedByIdWithRolesAsync(Guid userId, CancellationToken ct = default);
}