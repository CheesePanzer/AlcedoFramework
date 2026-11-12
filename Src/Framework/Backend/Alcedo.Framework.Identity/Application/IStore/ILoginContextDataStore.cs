namespace Alcedo.Framework.Identity.Application.IStore;

public interface ILoginContextRedisStore
{
    Task SaveAsync(string pendingToken, LoginContextData data, TimeSpan ttl);

    Task<LoginContextData?> GetAsync(string pendingToken);

    Task DeleteAsync(string pendingToken);
}

public record LoginContextData(Guid UserId);