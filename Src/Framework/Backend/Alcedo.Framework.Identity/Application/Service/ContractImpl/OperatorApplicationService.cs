using Alcedo.Framework.Identity.Application.IQueryRepository;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Exceptions;
using Alcedo.Framework.Shared.Application.Dto;

namespace Alcedo.Framework.Identity.Application.Service.ContractImpl;

internal sealed class OperatorModuleService : IOperatorModuleService
{
    private readonly IUserQueryRepository _userRepository;

    public OperatorModuleService(IUserQueryRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<OperatorDto> GetCurrentOperatorAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await TryGetCurrentOperatorAsync(userId, ct)
                   ?? throw new IdentityErrInvalidUser();

        return user;
    }

    public Task<OperatorDto?> TryGetCurrentOperatorAsync(Guid userId, CancellationToken ct = default)
    {
        return _userRepository.GetEnabledUnlockedByIdAsync(userId, ct);
    }

    public async Task<OperatorDtoWithRoles> GetCurrentOperatorWithRolesAsync(Guid userId,
        CancellationToken ct = default)
    {
        var user = await TryGetCurrentOperatorWithRolesAsync(userId, ct)
                   ?? throw new IdentityErrInvalidUser();

        return user;
    }

    public Task<OperatorDtoWithRoles?> TryGetCurrentOperatorWithRolesAsync(Guid userId, CancellationToken ct = default)
    {
        return _userRepository.GetEnabledUnlockedByIdWithRolesAsync(userId, ct);
    }
}