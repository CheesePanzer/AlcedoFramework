using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Framework.Identity.Application.Command;
using Alcedo.Framework.Identity.Application.Dto.Role;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Application.IQueryRepository;
using Alcedo.Framework.Identity.Application.IStore;
using Alcedo.Framework.Identity.Application.SearchQuery;
using Alcedo.Framework.Identity.Application.Service.Auth;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Identity.Domain.IRepository;
using Alcedo.Framework.Identity.Domain.Service;
using Alcedo.Framework.Identity.Infrastructure;
using Alcedo.Framework.Shared;
using Alcedo.Framework.Shared.Application.Components.MasterData;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Application.Service;
using Alcedo.Framework.Shared.Domain.Entity;
using System.Security.Cryptography;

namespace Alcedo.Framework.Identity.Application.Service.User;

public interface IUserApplicationService : IMasterDataApplicationService<UserDto, UserSearchQuery, UserCreateCommand,
    UserUpdateCommand>
{
    Task ArchiveByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default);

    Task LockUserAsync(Guid id, ContextOper oper, CancellationToken ct = default);

    Task UnlockUserAsync(Guid id, ContextOper oper, CancellationToken ct = default);

    Task<string> ResetPasswordAsync(Guid id, ContextOper oper, CancellationToken ct = default);

    Task BatchAssignRolesToUserAsync(Guid id, IReadOnlyCollection<Guid> roleIds, ContextOper oper,
        CancellationToken ct = default);

    Task BatchDeleteRolesForUserAsync(Guid id, IReadOnlyCollection<Guid> roleIds, ContextOper oper,
        CancellationToken ct = default);

    Task ClearRolesForUserAsync(Guid id, ContextOper oper, CancellationToken ct = default);

    Task DisableTwoFactorAuthenticationAsync(Guid id, ContextOper oper, CancellationToken ct = default);

    Task<PagedResult<RoleDto>> GetRoleDtosByUserIdAsync(Guid userId, PaginationQuery paginationQuery, CancellationToken ct = default);
}

[RegisterAs(typeof(IUserApplicationService))]
internal sealed class
    UserApplicationService : IUserApplicationService
{
    private readonly MasterDataSearchServiceComponent<UserAgg, UserDto, UserSearchQuery> _searchService;
    private readonly UserEditServiceComponent _editService;
    private readonly MasterDataStatusServiceComponent<UserAgg> _statusService;
    private readonly MasterDataLogSearchService<UserAgg> _logs;
    private readonly IAggRefChecker _aggRefChecker;
    private readonly IUnitOfWorkExecutor _executor;
    private readonly IUserRepository _userRepository;
    private readonly IAggRefChecker _refChecker;
    private readonly UserDomainService _userDomainService;
    private readonly ILoginThrottleRedisStore _loginThrottleRedisStore;
    private readonly IUserQueryRepository _userQueryRepository;
    private readonly LoginServiceComponent _loginService;


    public UserApplicationService(
        MasterDataSearchServiceComponent<UserAgg, UserDto, UserSearchQuery> searchService,
        UserEditServiceComponent editService,
        MasterDataStatusServiceComponent<UserAgg> statusService,
        IAggRefChecker aggRefChecker,
        IUnitOfWorkExecutor executor,
        IUserRepository userRepository,
        UserDomainService userDomainService,
        ILoginThrottleRedisStore loginThrottleRedisStore,
        IUserQueryRepository userQueryRepository,
        LoginServiceComponent loginService,
        MasterDataLogSearchService<UserAgg> logs,
        IAggRefChecker refChecker)
    {
        _searchService = searchService;
        _editService = editService;
        _statusService = statusService;
        _aggRefChecker = aggRefChecker;
        _executor = executor;
        _userRepository = userRepository;
        _userDomainService = userDomainService;
        _loginThrottleRedisStore = loginThrottleRedisStore;
        _userQueryRepository = userQueryRepository;
        _loginService = loginService;
        _logs = logs;
        _refChecker = refChecker;
    }


    public Task<PagedResult<UserDto>> GetPagedDtosAsync(UserSearchQuery query, SortQuery sortQuery,
        PaginationQuery pagiQuery, CancellationToken ct = default) =>
        _searchService.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);

    public Task<UserDto> GetDtoByIdAsync(Guid id, CancellationToken ct = default) =>
        _searchService.GetDtoByIdAsync(id, ct);

    public Task<Guid> CreateAsync(UserCreateCommand command, ContextOper oper, CancellationToken ct = default) =>
        _editService.CreateAsync(command, oper, ct);

    public Task UpdateByIdAsync(Guid id, UserUpdateCommand command, ContextOper oper, CancellationToken ct = default) =>
        _editService.UpdateByIdAsync(id, command, oper, ct);

    public Task DeleteByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default) =>
        _editService.DeleteByIdAsync(id, oper, ct);

    public Task EnableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default) =>
        _statusService.EnableByIdAsync(id, oper, ct);

    public async Task DisableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
    {
        await _refChecker.ThrowIfReferencedAsync<UserAgg>(id, ct);

        await _executor.ExecuteAsync(async () =>
            {
                var user = await _userRepository.GetByIdForUpdateAsync(id, ct)
                      ?? throw new DomErrResourceNotFound<UserAgg>(id);

                user.Disable(OperatorRecord.From(oper));

                user.LogOut();
            }, ct);

        await _loginService.ClearUserSessionAsync(id);
    }

    public Task<PagedResult<MasterDataAuditLogDto>> GetPagedAuditLogsAsync(MasterDataLogSearchQuery query,
        SortQuery sortQuery, PaginationQuery pagiQuery,
        CancellationToken ct = default)
        => _logs.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);

    public async Task ArchiveByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
    {
        await _executor.ExecuteAsync(async () =>
        {
            var userToBeArchived = await _userRepository.GetByIdForUpdateAsync(id, ct);

            if (userToBeArchived is null) // If it's missing, check if it's archived
            {
                if (await _userRepository.CheckIfAlreadyArchivedAsync(id, ct))
                {
                    return;
                }

                throw
                    new DomErrResourceNotFound<UserAgg>(
                        id); // If not archived and not exists in user, then this id is invalid
            }

            userToBeArchived.LogOut();

            await _aggRefChecker.ThrowIfReferencedAsync<UserAgg>(id, ct);

            userToBeArchived.Archive(OperatorRecord.From(oper));

            await _userRepository.ArchiveHistoryUserAsync(
                new HistoryUserEntity(userToBeArchived, OperatorRecord.From(oper)), ct);

            _userRepository.Remove(userToBeArchived);
        }, ct);

        //强制下线
        await _loginService.ClearUserSessionAsync(id);
    }

    /// <summary>
    /// 锁定用户
    /// </summary>
    /// <param name="id"></param>
    /// <param name="operId"></param>
    public async Task LockUserAsync(Guid id, ContextOper oper, CancellationToken ct = default)
    {
        await _executor.ExecuteAsync(async () =>
        {
            var userToBeUpdated = await _userRepository.GetByIdForUpdateAsync(id, ct) ??
                                  throw new DomErrResourceNotFound<UserAgg>(id);

            userToBeUpdated.Lock(OperatorRecord.From(oper));

            userToBeUpdated.LogOut();
        }, ct);
        //强制下线
        await _loginService.ClearUserSessionAsync(id);
    }

    /// <summary>
    /// 解锁用户
    /// </summary>
    /// <param name="id"></param>
    /// <param name="operId"></param>
    public async Task UnlockUserAsync(Guid id, ContextOper oper, CancellationToken ct = default)
    {
        var userName = await _executor.ExecuteAsync(async () =>
        {
            var userToBeUpdated = await _userRepository.GetByIdForUpdateAsync(id, ct) ??
                                  throw new DomErrResourceNotFound<UserAgg>(id);

            userToBeUpdated.Unlock(OperatorRecord.From(oper));

            return userToBeUpdated.LoginName;
        }, ct);

        await _loginThrottleRedisStore.ResetFailureCountByLoginNameAsync(userName);
    }

    /// <summary>
    /// 重置密码,同时解锁因密码错误被锁定的用户
    /// </summary>
    /// <param name="id"></param>
    /// <param name="operId"></param>
    /// <returns></returns>
    /// <exception cref="BErrorInvalidUser"></exception>
    /// <exception cref="BErrorAggNotFound{UserAggregate}"></exception>
    public async Task<string> ResetPasswordAsync(Guid id, ContextOper oper, CancellationToken ct = default)
    {
        static string GenerateRandomPassword(int length = 16)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";
            return RandomNumberGenerator.GetString(chars, length);
        }


        (string loginName, string newPassword) result = await _executor.ExecuteAsync(async () =>
        {
            var userToBeUpdated = await _userRepository.GetByIdForUpdateAsync(id, ct) ??
                                  throw new DomErrResourceNotFound<UserAgg>(id);

            var newPlainPassword = GenerateRandomPassword(8);

            var newHashedPassword = PasswordHasher.Hash(newPlainPassword);

            userToBeUpdated.ResetPassword(newHashedPassword, OperatorRecord.From(oper));

            userToBeUpdated.LogOut();

            return (userToBeUpdated.LoginName, newPlainPassword);
        }, ct);
        //强制下线，同时重置失败次数
        await _loginService.ClearUserSessionAsync(id);
        await _loginThrottleRedisStore.ResetFailureCountByLoginNameAsync(result.loginName);

        return result.newPassword;
    }

    /// <summary>
    /// 批量添加角色，会检验角色的存在和是否启用,没启用会提示，由于允许用户在禁用的用户组中，所以查询时不加锁，不强制要求并发一致性
    /// </summary>
    /// <param name="id"></param>
    /// <param name="roleIds"></param>
    /// <param name="operId"></param>
    /// <exception cref="BErrorInvalidUser"></exception>
    /// <exception cref="BErrorAggNotFound{UserAggregate}"></exception>
    /// <exception cref="BErrorMaterDataNotFoundOrDisabled{RoleAggregate}">返回未启用的角色列表</exception>
    public async Task BatchAssignRolesToUserAsync(Guid id, IReadOnlyCollection<Guid> roleIds, ContextOper oper,
        CancellationToken ct = default)
    {
        await _executor.ExecuteAsync(
            () => _userDomainService.BatchAssignRolesToUserAsync(id, roleIds, OperatorRecord.From(oper), ct), ct);
    }

    /// <summary>
    /// 从用户中删除角色关系
    /// </summary>
    /// <param name="id"></param>
    /// <param name="roleId"></param>
    /// <param name="operId"></param>
    /// <exception cref="BErrorInvalidUser"></exception>
    /// <exception cref="BErrorAggNotFound{UserAggregate}"></exception>
    public async Task BatchDeleteRolesForUserAsync(Guid id, IReadOnlyCollection<Guid> roleIds, ContextOper oper,
        CancellationToken ct = default)
    {
        await _executor.ExecuteAsync(async () =>
        {
            var userToBeUpdated = await _userRepository.GetByIdForUpdateAsync(id, ct) ??
                                  throw new DomErrResourceNotFound<UserAgg>(id);

            userToBeUpdated.BatchDeleteRoles(roleIds, OperatorRecord.From(oper));
        }, ct);
    }

    /// <summary>
    /// 清空用户角色
    /// </summary>
    /// <param name="id"></param>
    /// <param name="operId"></param>
    /// <exception cref="BErrorInvalidUser"></exception>
    /// <exception cref="BErrorAggNotFound{UserAggregate}"></exception>
    public async Task ClearRolesForUserAsync(Guid id, ContextOper oper, CancellationToken ct = default)
    {
        await _executor.ExecuteAsync(async () =>
        {
            var userToBeUpdated = await _userRepository.GetByIdForUpdateAsync(id, ct) ??
                                  throw new DomErrResourceNotFound<UserAgg>(id);

            userToBeUpdated.ClearRoles(OperatorRecord.From(oper));
        }, ct);
    }

    public async Task DisableTwoFactorAuthenticationAsync(Guid id, ContextOper oper, CancellationToken ct = default)
    {
        await _executor.ExecuteAsync(async () =>
        {
            var userToBeUpdated = await _userRepository.GetByIdForUpdateAsync(id, ct) ??
                                  throw new DomErrResourceNotFound<UserAgg>(id);

            userToBeUpdated.DisableTwoFactorAuthenticator(OperatorRecord.From(oper));

            userToBeUpdated.LogOut();
        }, ct);
        await _loginService.ClearUserSessionAsync(id);
    }

    public async Task<PagedResult<RoleDto>> GetRoleDtosByUserIdAsync(Guid userId, PaginationQuery paginationQuery,
        CancellationToken ct = default)
    {
        var result = await _userQueryRepository.GetRoleDtosByUserIdAsync(userId, paginationQuery, ct);

        return new PagedResult<RoleDto>
        {
            Items = result.Items,
            PageNo = paginationQuery.PageNo,
            PageSize = paginationQuery.PageSize,
            TotalCount = result.Count,
        };
    }
}
