namespace Alcedo.Framework.Identity.Application.IStore;

public interface ILoginThrottleRedisStore
{
    Task<long> IncrementFailureCountByLoginNameAsync(string loginName);

    Task<long> GetFailureCountByLoginNameAsync(string loginName);

    Task ResetFailureCountByLoginNameAsync(string loginName);
}