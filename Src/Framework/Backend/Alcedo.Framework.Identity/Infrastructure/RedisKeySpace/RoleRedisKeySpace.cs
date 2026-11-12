using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure;
using Alcedo.Core.Infrastructure.Redis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Alcedo.Framework.Identity.Infrastructure.RedisKeySpace;

public sealed class RoleCacheRedisKeySpace : RedisKeySpaceBase
{
    public RoleCacheRedisKeySpace(IConnectionMultiplexer multiplexer, IOptions<RedisOptions> options,
        AlcedoModulesRegistry registry) : base(multiplexer, options, registry)
    {
    }
}