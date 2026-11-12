using Alcedo.Core.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Alcedo.Core.Infrastructure.Redis.KeySpace;

public sealed class IdempotencyKeyRedisKeySpace : RedisKeySpaceBase
{
    public IdempotencyKeyRedisKeySpace(IConnectionMultiplexer multiplexer, IOptions<RedisOptions> options,
        AlcedoModulesRegistry registry) : base(multiplexer, options, registry)
    {
    }
}