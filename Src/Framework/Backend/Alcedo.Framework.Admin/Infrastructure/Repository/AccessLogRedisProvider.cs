using System.Text.Json;
using Alcedo.Core.Abstractions;
using Alcedo.Framework.Admin.Domain.Entity;
using Alcedo.Framework.Admin.Infrastructure.RedisKeySpace;
using Alcedo.Framework.Admin.Presentation.Interface;
using StackExchange.Redis;

namespace Alcedo.Framework.Admin.Infrastructure.Repository;

[RegisterAs(typeof(IAccessLogRedisStore))]
internal sealed class AccessLogRedisProvider : IAccessLogRedisStore
{
    private readonly IDatabase _redisDb;

    private const string RedisKey = "Queue";

    public AccessLogRedisProvider(
        AccessLogRedisKeySpace redis)
    {
        _redisDb = redis.Db;
    }

    public async Task EnqueueLogAsync(AccessLogEntity log)
    {
        await _redisDb.ListRightPushAsync(RedisKey,
            JsonSerializer.Serialize(log));
    }

    public async Task<IReadOnlyList<string>> DequeueRawLogsAsync(int batchSize)
    {
        var results = await _redisDb.ListLeftPopAsync(RedisKey, batchSize);

        if (results is null)
        {
            return [];
        }

        return results
            .Where(v => !v.IsNullOrEmpty)
            .Select(v => v.ToString())
            .ToList();
    }

    public async Task RequeueRawLogsAsync(IEnumerable<string> logs)
    {
        var values = logs.Select(l => (RedisValue)l).ToArray();
        if (values.Length == 0)
        {
            return;
        }

        await _redisDb.ListRightPushAsync(RedisKey, values);
    }
}