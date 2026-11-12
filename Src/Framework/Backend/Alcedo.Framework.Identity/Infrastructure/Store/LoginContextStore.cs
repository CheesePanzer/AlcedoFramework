using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure.Redis;
using Alcedo.Framework.Identity.Application.IStore;
using Alcedo.Framework.Identity.Infrastructure.RedisKeySpace;
using Microsoft.Extensions.Logging;

namespace Alcedo.Framework.Identity.Infrastructure.Store;

[RegisterAs(typeof(ILoginContextRedisStore))]
public class LoginContextRedisStore : RedisJsonStoreBase<LoginContextRedisKeySpace, LoginContextData>,
    ILoginContextRedisStore
{
    public LoginContextRedisStore(LoginContextRedisKeySpace keySpace, ILogger<LoginContextRedisStore> logger) : base(
        keySpace,
        logger)
    {
    }

    public Task SaveAsync(string pendingToken, LoginContextData data, TimeSpan ttl)
        => SetValueAsync(pendingToken, data, ttl);

    public Task<LoginContextData?> GetAsync(string pendingToken)
        => GetValueAsync(pendingToken);

    public Task DeleteAsync(string pendingToken)
        => DeleteValueAsync(pendingToken);
}