using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure;
using Alcedo.Core.Infrastructure.Redis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Alcedo.Framework.Admin.Infrastructure.RedisKeySpace;

public class AccessLogRedisKeySpace : RedisKeySpaceBase
{
    public AccessLogRedisKeySpace(IConnectionMultiplexer multiplexer, IOptions<RedisOptions> options,
        AlcedoModulesRegistry registry) : base(multiplexer, options, registry)
    {
    }
}