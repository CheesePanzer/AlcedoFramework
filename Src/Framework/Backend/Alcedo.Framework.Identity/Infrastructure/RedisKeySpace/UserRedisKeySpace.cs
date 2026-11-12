using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure;
using Alcedo.Core.Infrastructure.Redis;
using Alcedo.Framework.Identity.Application.Dto.User;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Alcedo.Framework.Identity.Infrastructure.RedisKeySpace;

[RegisterAsEntityCacheStore(typeof(UserCacheDto))]
public sealed class UserCacheRedisKeySpace : RedisKeySpaceBase
{
    public UserCacheRedisKeySpace(IConnectionMultiplexer multiplexer, IOptions<RedisOptions> options,
        AlcedoModulesRegistry registry) : base(multiplexer, options, registry)
    {
    }
}