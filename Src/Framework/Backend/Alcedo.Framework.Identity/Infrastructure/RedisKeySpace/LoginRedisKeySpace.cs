using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure;
using Alcedo.Core.Infrastructure.Redis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Alcedo.Framework.Identity.Infrastructure.RedisKeySpace;

public sealed class LoginContextRedisKeySpace : RedisKeySpaceBase
{
    public LoginContextRedisKeySpace(IConnectionMultiplexer multiplexer, IOptions<RedisOptions> options,
        AlcedoModulesRegistry registry) : base(multiplexer, options, registry)
    {
    }
}

public sealed class LoginThrottleRedisKeySpace : RedisKeySpaceBase
{
    public LoginThrottleRedisKeySpace(IConnectionMultiplexer multiplexer, IOptions<RedisOptions> options,
        AlcedoModulesRegistry registry) : base(multiplexer, options, registry)
    {
    }
}