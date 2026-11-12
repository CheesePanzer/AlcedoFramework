using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure.Redis;
using Alcedo.Framework.Identity.Application.IStore;
using Alcedo.Framework.Identity.Infrastructure.RedisKeySpace;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alcedo.Framework.Identity.Infrastructure.Store;

[RegisterAs(typeof(ILoginThrottleRedisStore))]
internal sealed class LoginThrottleRedisStore : RedisStoreBase<LoginThrottleRedisKeySpace>, ILoginThrottleRedisStore
{
    private readonly TimeSpan _expiry;

    public LoginThrottleRedisStore(LoginThrottleRedisKeySpace redis, IOptions<SecurityOptions> securityOptions,
        ILogger<LoginThrottleRedisStore> logger) :
        base(redis, logger)
    {
        _expiry = TimeSpan.FromSeconds(securityOptions.Value.LoginFailedTTLSeconds);
    }

    public async Task<long> IncrementFailureCountByLoginNameAsync(string loginName)
    {
        var newValue = await _db.StringIncrementAsync(loginName);

        if (newValue == 1)
        {
            await _db.KeyExpireAsync(loginName, _expiry);
        }

        return newValue;
    }

    public async Task<long> GetFailureCountByLoginNameAsync(string loginName)
    {
        var value = await _db.StringGetAsync(loginName);

        return value.HasValue ? (long)value : 0;
    }

    public Task ResetFailureCountByLoginNameAsync(string loginName)
        => DeleteValueAsync(loginName);
}