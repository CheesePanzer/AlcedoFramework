namespace Alcedo.Framework.Identity.Contracts;

public interface IUserQueryIdentityService
{
    Task<UserInfoDto> GetUserInfoAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyCollection<UserInfoDto>> GetUserInfosByIdsStrictAsync(IReadOnlyCollection<Guid> userIds,
        CancellationToken ct = default);
}