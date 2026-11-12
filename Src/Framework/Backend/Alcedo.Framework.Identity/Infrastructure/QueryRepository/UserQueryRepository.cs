using System.Data;
using Alcedo.Core.Abstractions;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Core.Infrastructure.QueryRepositoryBase;
using Alcedo.Framework.Identity.Application.Dto.Role;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Application.IQueryRepository;
using Alcedo.Framework.Identity.Application.SearchQuery;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Domain.Entity;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.Identity.Infrastructure.QueryRepository;

[RegisterAs(typeof(IUserQueryRepository))]
internal sealed class UserQueryRepository : EntityQueryRepositoryBase<UserAgg, UserDto, UserSearchQuery>,
    IUserQueryRepository
{
    public UserQueryRepository(AlcedoDbContext context) : base(context)
    {
    }

    public async Task<(int Count,IReadOnlyList<RoleDto> Items)> GetRoleDtosByUserIdAsync(Guid userId, PaginationQuery paginationQuery, CancellationToken ct = default)
    {
        await using var tx = await Context.Database
            .BeginTransactionAsync(IsolationLevel.RepeatableRead, ct); // For the consistency of count and data;
        
        var totalCount = await Context.RawEntity<UserRolesRelation>()
            .AsNoTracking()
            .Where(u => u.UserId == userId).CountAsync(ct);
        
        var roles = await Context.AggRoot<UserAgg>()
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .SelectMany(u => u.UserRoles)
            .Join(Context.AggRoot<RoleAgg>(),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r)
            .Skip((paginationQuery.PageNo - 1) * paginationQuery.PageSize)
            .Take(paginationQuery.PageSize)
            .ProjectToType<RoleDto>()
            .ToListAsync(ct);
        
        return (totalCount, roles);
    }

    public Task<OperatorDto?> GetEnabledUnlockedByIdAsync(Guid userId, CancellationToken ct = default)
    {
        return DbSet
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsLocked && u.Status == MasterDataStates.Enabled)
            .Select(u => new OperatorDto()
            {
                Id = u.Id,
                Code = u.Code,
                Name = u.Name
            }).SingleOrDefaultAsync(ct);
    }

    public Task<OperatorDtoWithRoles?> GetEnabledUnlockedByIdWithRolesAsync(Guid userId, CancellationToken ct = default)
    {
        return DbSet
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsLocked && u.Status == MasterDataStates.Enabled)
            .Select(u => new OperatorDtoWithRoles
            {
                Id = u.Id,
                Code = u.Code,
                Name = u.Name,
                IsRootUser = u.IsRootUser,
                RoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList()
            })
            .SingleOrDefaultAsync(ct);
    }
}