using Alcedo.Core.Abstractions;
using Alcedo.Core.Domain.DomException;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Identity.Domain.IRepository;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;

namespace Alcedo.Framework.Identity.Domain.Service;

[RegisterSelf]
internal sealed class UserDomainService
{
    private readonly IUserRepository _userRepository;

    private readonly IMasterDataRepositoryBase<RoleAgg> _roleRepository;


    public UserDomainService(IUserRepository userRepository, IMasterDataRepositoryBase<RoleAgg> roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task BatchAssignRolesToUserAsync(Guid userId, IReadOnlyCollection<Guid> roleIds, OperatorRecord oper,
        CancellationToken ct = default)
    {
        var userToBeUpdated = await _userRepository.GetByIdForUpdateAsync(userId, ct) ??
                              throw new DomErrResourceNotFound<UserAgg>(userId);

        var roles = await _roleRepository
            .GetEnabledListStrictByIdsForShareAsync(roleIds, ct);

        userToBeUpdated.BatchAssignRoles(roles.Select(r => r.Id).ToList(), oper);
    }
}