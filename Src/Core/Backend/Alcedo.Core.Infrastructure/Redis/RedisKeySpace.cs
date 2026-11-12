using Alcedo.Core.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;

namespace Alcedo.Core.Infrastructure.Redis;

public abstract class RedisKeySpaceBase
{
    protected RedisKeySpaceBase(IConnectionMultiplexer multiplexer, IOptions<RedisOptions> options,
        AlcedoModulesRegistry registry)
    {
        var prefix = GetType().Name.Replace("RedisKeySpace", string.Empty);

        var moduleCode = registry.GetModuleCodeByAssembly(GetType().Assembly);

        if (moduleCode is null)
        {
            Db = multiplexer.GetDatabase()
                .WithKeyPrefix($"{options.Value.GlobalPrefix}:{prefix}:");
        }
        else
        {
            Db = multiplexer.GetDatabase()
                .WithKeyPrefix($"{options.Value.GlobalPrefix}:{moduleCode}:{prefix}:");
        }
    }

    public IDatabase Db { get; }
}