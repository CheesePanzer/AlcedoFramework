using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Core.Infrastructure.QueryRepositoryBase;
using Alcedo.Core.Presentation;
using Alcedo.Framework.Identity.Application.Dto;
using Alcedo.Framework.Identity.Application.Dto.Role;
using Alcedo.Framework.Identity.Application.IQueryRepository;
using Alcedo.Framework.Identity.Application.SearchQuery;
using Alcedo.Framework.Identity.Domain.Entity;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.Identity.Infrastructure.QueryRepository;

[RegisterAs(typeof(IRoleQueryRepository))]
internal sealed class RoleQueryRepository : EntityQueryRepositoryBase<RoleAgg, RoleDto, RoleSearchQuery>,
    IRoleQueryRepository
{
    private readonly EndpointResourceOperationCenter _center;

    private readonly IMapper _mapper;

    public RoleQueryRepository(AlcedoDbContext context, EndpointResourceOperationCenter center, IMapper mapper) :
        base(context)
    {
        _center = center;
        _mapper = mapper;
    }

    public async Task<PagedResult<PermDto>> GetPermsByRoleIdAsync(
        Guid roleId,
        PaginationQuery pagiQuery,
        CancellationToken ct = default)
    {
        var role = await DbSet.SingleOrDefaultAsync(r => r.Id == roleId, cancellationToken: ct) ??
                   throw new DomErrResourceNotFound<RoleAgg>(roleId);

        var result =
            _center.GetPagedEndpointMetadataByKeyList(pagiQuery.PageNo, pagiQuery.PageSize, role.AllowedEndpoints);

        var dtos = _mapper.Map<PermDto[]>(result.Metas);

        return new PagedResult<PermDto>
        {
            Items = dtos,
            PageNo = pagiQuery.PageNo,
            PageSize = pagiQuery.PageSize,
            TotalCount = result.Count
        };
    }
}