using Alcedo.Core.Abstractions;
using Alcedo.Framework.Identity.Application.IQueryRepository;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Exceptions;
using MapsterMapper;

namespace Alcedo.Framework.Identity.Application.Service.ContractImpl;

[RegisterAs(typeof(IUserQueryIdentityService))]
internal sealed class UserInfoApplicationService : IUserQueryIdentityService
{
    private readonly IUserQueryRepository _userQueryRepository;

    private readonly IMapper _mapper;

    public UserInfoApplicationService(IUserQueryRepository userQueryRepository, IMapper mapper)
    {
        _userQueryRepository = userQueryRepository;
        _mapper = mapper;
    }

    public async Task<UserInfoDto> GetUserInfoAsync(Guid userId, CancellationToken ct = default)
    {
        var dto = await _userQueryRepository.GetByIdAsync(userId, ct) ??
                  throw new IdentityErrUserNotFound(userId);
        return _mapper.Map<UserInfoDto>(dto);
    }

    public async Task<IReadOnlyCollection<UserInfoDto>> GetUserInfosByIdsStrictAsync(IReadOnlyCollection<Guid> userIds,
        CancellationToken ct = default)
    {
        var dtos = await _userQueryRepository.GetListByIdsAsync(userIds, ct) ??
                   throw new IdentityErrUserNotFound(userIds);

        var notFound = userIds.Except(dtos.Select(u => u.Id)).ToList();

        if (notFound.Any())
        {
            throw new IdentityErrUserNotFound(notFound);
        }

        return _mapper.Map<IReadOnlyCollection<UserInfoDto>>(dtos);
    }
}