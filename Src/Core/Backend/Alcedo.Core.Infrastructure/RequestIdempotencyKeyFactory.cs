using Alcedo.Core.Application.Tools;
using Alcedo.Core.Infrastructure.Redis.KeySpace;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Alcedo.Core.Infrastructure;

public class IdempotencyKeyProvider : IIdempotencyKeyProvider
{
    private readonly IDatabase _redisDb;

    private readonly IdempotencyKeyOptions _options;

    public IdempotencyKeyProvider(
        IdempotencyKeyRedisKeySpace redis,
        IOptions<IdempotencyKeyOptions> formTokenOptions)
    {
        _redisDb = redis.Db;

        _options = formTokenOptions.Value;
    }

    public async Task<string> GenerateStrictIdempotencyKeyAsync(string endpoint, int? expireyMinutes)
    {
        var token = Guid.NewGuid().ToString("N");
        var redisKey = $"Strict:{endpoint}:{token}";

        await _redisDb.StringSetAsync(redisKey, "1",
            TimeSpan.FromMinutes(expireyMinutes ?? _options.StrictKeyExpireMinutes));
        return token;
    }

    public async Task<bool> ValidateAndConsumeStrictGeneratedIdempotencyKeyAsync(string endpoint, string token)
    {
        var redisKey = $"Strict:{endpoint}:{token}";

        return await _redisDb.KeyDeleteAsync(redisKey);
    }

    public async Task<bool> ValidateContentBasedIdempotencyKeyAsync(string contentMd, string endpoint,
        int? expiryMinutes)
    {
        var redisKey = $"ContentBased:{endpoint}:{contentMd}";

        return await _redisDb.StringSetAsync(
            redisKey,
            "1",
            TimeSpan.FromMinutes(expiryMinutes ?? _options.CommonKeyExpireMinutes),
            when: When.NotExists
        );
    }

    public async Task<bool> ValidateAndRegisterSoftIdempotencyKeyAsync(string token, string endpoint,
        int? expiryMinutes)
    {
        var redisKey = $"Soft:{endpoint}:{token}";

        return await _redisDb.StringSetAsync(
            redisKey,
            "1",
            TimeSpan.FromMinutes(expiryMinutes ?? _options.CommonKeyExpireMinutes),
            when: When.NotExists
        );
    }
}