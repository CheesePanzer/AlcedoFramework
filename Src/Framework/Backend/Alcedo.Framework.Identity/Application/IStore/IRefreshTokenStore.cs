namespace Alcedo.Framework.Identity.Application.IStore;

public interface IRefreshTokenRedisStore
{
    Task SetTokenForUserIdAsync(Guid userId, Guid token, TimeSpan ttl);

    Task RotateTokenForUserIdAsync(Guid userId, Guid oldToken, Guid newToken);

    Task<Guid?> ValidateTokenAsync(Guid token);

    Task RemoveByUserIdAsync(Guid userId);

    Task<Guid?> RemoveByRefreshTokenAsync(Guid token);
}