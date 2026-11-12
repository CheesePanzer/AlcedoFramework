using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.DomException;
using Alcedo.Framework.Identity.Application.Command;
using Alcedo.Framework.Identity.Application.Dto;
using Alcedo.Framework.Identity.Application.Dto.Role;
using Alcedo.Framework.Identity.Application.IQueryRepository;
using Alcedo.Framework.Identity.Application.Provider;
using Alcedo.Framework.Identity.Application.SearchQuery;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Identity.Domain.Service;
using Alcedo.Framework.Identity.Exceptions;
using Alcedo.Framework.Shared;
using Alcedo.Framework.Shared.Application;
using Alcedo.Framework.Shared.Application.Components.MasterData;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Application.Service;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;

namespace Alcedo.Framework.Identity.Application.Service.Role;

public interface IRoleApplicationService : IMasterDataApplicationService<RoleDto, RoleSearchQuery, RoleCreateCommand,
    RoleUpdateCommand>
{
    Task BatchAssignPermsToRoleAsync(Guid roleId, IReadOnlyCollection<string> permKeys, Guid operId,
        CancellationToken ct = default);

    Task BatchDeletePermsForRoleAsync(Guid roleId, IReadOnlyCollection<string> permKeys, ContextOper oper,
        CancellationToken ct = default);

    Task ClearPermsForRoleAsync(Guid roleId, ContextOper oper, CancellationToken ct = default);

    Task RefreshRoleCacheByIdAsync(Guid roleId, CancellationToken ct = default);

    Task<PagedResult<PermDto>> GetPermsByRoleIdAsync(
        Guid roleId,
        PaginationQuery pagiQuery,
        CancellationToken ct = default);
}

[RegisterAs(typeof(IRoleApplicationService))]
internal sealed class RoleApplicationService
    : IRoleApplicationService
{
    private readonly MasterDataSearchServiceComponent<RoleAgg, RoleDto, RoleSearchQuery> _roleSearchServiceComponent;
    private readonly RoleEditServiceComponent _roleEditServiceComponent;
    private readonly RoleStatusServiceComponent _roleStatusServiceComponent;
    private readonly MasterDataLogSearchService<RoleAgg> _log;
    private readonly IRoleQueryRepository _roleQueryRepository;
    private readonly RoleDomainService _roleDomainService;
    private readonly IOperatorModuleService _operatorService;
    private readonly IUnitOfWorkExecutor _executor;
    private readonly IMasterDataRepositoryBase<RoleAgg> _roleRepository;
    private readonly IRoleCacheProvider _roleCacheProvider;

    public RoleApplicationService(
        MasterDataSearchServiceComponent<RoleAgg, RoleDto, RoleSearchQuery> roleSearchServiceComponent,
        RoleEditServiceComponent roleEditServiceComponent,
        RoleStatusServiceComponent roleStatusServiceComponent,
        IUnitOfWorkExecutor unitOfWorkExecutor,
        IMasterDataRepositoryBase<RoleAgg> roleRepository,
        IOperatorModuleService operatorService,
        RoleDomainService roleDomainService,
        IRoleCacheProvider roleCacheProvider,
        IRoleQueryRepository roleQueryRepository,
        MasterDataLogSearchService<RoleAgg> log)
    {
        _roleSearchServiceComponent = roleSearchServiceComponent;
        _roleEditServiceComponent = roleEditServiceComponent;
        _roleStatusServiceComponent = roleStatusServiceComponent;
        _executor = unitOfWorkExecutor;
        _roleRepository = roleRepository;
        _operatorService = operatorService;
        _roleDomainService = roleDomainService;
        _roleCacheProvider = roleCacheProvider;
        _roleQueryRepository = roleQueryRepository;
        _log = log;
    }


    public Task<PagedResult<RoleDto>> GetPagedDtosAsync(RoleSearchQuery query, SortQuery sortQuery,
        PaginationQuery pagiQuery,
        CancellationToken ct = default) =>
        _roleSearchServiceComponent.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);

    public Task<RoleDto> GetDtoByIdAsync(Guid id, CancellationToken ct = default) =>
        _roleSearchServiceComponent.GetDtoByIdAsync(id, ct);

    public Task<Guid> CreateAsync(RoleCreateCommand command, ContextOper oper, CancellationToken ct = default) =>
        _roleEditServiceComponent.CreateAsync(command, oper, ct);

    public Task UpdateByIdAsync(Guid id, RoleUpdateCommand command, ContextOper oper, CancellationToken ct = default) =>
        _roleEditServiceComponent.UpdateByIdAsync(id, command, oper, ct);

    public async Task DeleteByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
    {
        await _roleEditServiceComponent.DeleteByIdAsync(id, oper, ct);
        await _roleCacheProvider.DeleteAsync(id);
    }

    public Task RefreshRoleCacheByIdAsync(Guid roleId, CancellationToken ct = default) =>
        _roleCacheProvider.RefreshFromDbAsync(roleId, ct: ct);

    public Task EnableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default) =>
        _roleStatusServiceComponent.EnableByIdAsync(id, oper, ct);

    public Task DisableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default) =>
        _roleStatusServiceComponent.DisableByIdAsync(id, oper, ct);

    public Task<PagedResult<MasterDataAuditLogDto>> GetPagedAuditLogsAsync(MasterDataLogSearchQuery query,
        SortQuery sortQuery, PaginationQuery pagiQuery,
        CancellationToken ct = default)
        => _log.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);

    /// <summary>
    /// 给角色批量添加权限
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="permKeys"></param>
    /// <param name="operId"></param>
    /// <param name="ct"></param>
    /// <exception cref="BErrorInvalidUser"></exception>
    /// <exception cref="BErrorAggNotFound{RoleAggregate}"></exception>
    /// <exception cref="BErrorMaterDataNotFoundOrDisabled{PermissionEntity}"></exception>
    public async Task BatchAssignPermsToRoleAsync(Guid roleId, IReadOnlyCollection<string> permKeys, Guid operId,
        CancellationToken ct = default)
    {
        var oper = await _operatorService.GetCurrentOperatorWithRolesAsync(operId, ct);

        if (!oper.IsRootUser)
        {
            var role = await _roleRepository.GetListStrictByIdsNoTrackingAsync(oper.RoleIds, ct);
            var hasPermKeys = role
                .SelectMany(r => r.AllowedEndpoints)
                .ToHashSet();

            var notHavingPermKeys = permKeys
                .Where(k => !hasPermKeys.Contains(k))
                .ToList();

            if (notHavingPermKeys.Count > 0)
            {
                throw new IdentityErrNotPermittedPartially("You can't assign permissions that you don't have.");
            }
        }

        await _executor.ExecuteAsync(
            () => _roleDomainService.AssignPermsToRoleAsync(roleId, permKeys,
                new OperatorRecord(oper.Id, oper.Code, oper.Name), ct), ct);
    }

    /// <summary>
    /// 删除角色内某个权限
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="permKeys"></param>
    /// <param name="oper"></param>
    /// <param name="ct"></param>
    /// <exception cref="BErrorInvalidUser"></exception>
    /// <exception cref="BErrorAggNotFound{RoleAggregate}"></exception>
    public async Task BatchDeletePermsForRoleAsync(Guid roleId, IReadOnlyCollection<string> permKeys, ContextOper oper,
        CancellationToken ct = default)
    {
        await _executor.ExecuteAsync(async () =>
        {
            var role = await _roleRepository.GetByIdForUpdateAsync(roleId, ct) ??
                       throw new DomErrResourceNotFound<RoleAgg>(roleId);

            role.BatchDeletePermissions(permKeys, OperatorRecord.From(oper));
        }, ct);
    }

    /// <summary>
    /// 完全清空角色的权限
    /// </summary>
    /// <param name="roleId"></param>
    /// <param name="oper"></param>
    /// <param name="ct"></param>
    /// <exception cref="BErrorInvalidUser"></exception>
    /// <exception cref="BErrorAggNotFound{RoleAggregate}"></exception>
    public async Task ClearPermsForRoleAsync(Guid roleId, ContextOper oper, CancellationToken ct = default)
    {
        await _executor.ExecuteAsync(async () =>
        {
            var role = await _roleRepository.GetByIdForUpdateAsync(roleId, ct) ??
                       throw new DomErrResourceNotFound<RoleAgg>(roleId);

            role.ClearPermissions(OperatorRecord.From(oper));
        }, ct);
    }

    public Task<PagedResult<PermDto>> GetPermsByRoleIdAsync(
        Guid roleId,
        PaginationQuery pagiQuery,
        CancellationToken ct = default) => _roleQueryRepository.GetPermsByRoleIdAsync(roleId, pagiQuery, ct);
}