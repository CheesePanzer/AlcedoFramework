using System.Security.Cryptography;
using System.Text;
using Alcedo.Core.Abstractions;
using Alcedo.Framework.Identity.Application.IStore;
using Alcedo.Framework.Identity.Exceptions;
using Alcedo.Framework.Identity.Infrastructure.RedisKeySpace;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Alcedo.Framework.Identity.Infrastructure.Store;

[RegisterAs(typeof(IRefreshTokenRedisStore))]
internal class RefreshTokenRedisStore : IRefreshTokenRedisStore
{
    private readonly IDatabase _db;
    private readonly ILogger<RefreshTokenRedisStore> _logger;

    public RefreshTokenRedisStore(RefreshTokenRedisKeySpace redis, ILogger<RefreshTokenRedisStore> logger)
    {
        _logger = logger;
        _db = redis.Db;
    }

    private static string UserKey(Guid userId) => $"User:{userId}";
    private static string TokenKey(string tokenHash) => $"Token:{tokenHash}";

    private static string HashToken(Guid token)
    {
        var bytes = Encoding.UTF8.GetBytes(token.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task SetTokenForUserIdAsync(Guid userId, Guid token, TimeSpan ttl)
    {
        var tokenHash = HashToken(token);
        var oldToken = await _db.StringGetAsync(UserKey(userId));

        if (!oldToken.IsNullOrEmpty)
        {
            await _db.KeyDeleteAsync(TokenKey(oldToken!));
        }

        await _db.StringSetAsync(UserKey(userId), tokenHash, ttl);
        await _db.StringSetAsync(TokenKey(tokenHash), userId.ToString(), ttl);
    }

    public async Task RotateTokenForUserIdAsync(Guid userId, Guid oldToken, Guid newToken)
    {
        var remainingTtl = await _db.KeyTimeToLiveAsync(UserKey(userId));
        if (remainingTtl is null || remainingTtl.Value <= TimeSpan.Zero)
        {
            throw new IdentityErrLoginStatusExpired(userId, "expired");
        }

        var storedHash = await _db.StringGetAsync(UserKey(userId));
        if (storedHash != HashToken(oldToken))
        {
            throw new IdentityErrLoginStatusExpired(userId, "mismatched");
        }

        await _db.KeyDeleteAsync(TokenKey(HashToken(oldToken)));
        await _db.StringSetAsync(UserKey(userId), HashToken(newToken), remainingTtl.Value);
        await _db.StringSetAsync(TokenKey(HashToken(newToken)), userId.ToString(), remainingTtl.Value);
    }

    public async Task<Guid?> ValidateTokenAsync(Guid token)
    {
        var userIdString = await _db.StringGetAsync(TokenKey(HashToken(token)));
        if (userIdString.IsNullOrEmpty)
        {
            return null;
        }

        return Guid.TryParse(userIdString.ToString(), out var userId) ? userId : null;
    }

    public async Task RemoveByUserIdAsync(Guid userId)
    {
        var token = await _db.StringGetAsync(UserKey(userId));
        if (!token.IsNullOrEmpty)
        {
            await _db.KeyDeleteAsync(TokenKey(token!));
        }

        await _db.KeyDeleteAsync(UserKey(userId));
    }

    public async Task<Guid?> RemoveByRefreshTokenAsync(Guid token)
    {
        var tokenHash = HashToken(token);
        var userIdString = await _db.StringGetAsync(TokenKey(tokenHash));

        if (userIdString.IsNullOrEmpty)
        {
            return null;
        }

        await _db.KeyDeleteAsync(TokenKey(tokenHash));

        if (Guid.TryParse(userIdString.ToString(), out var userId))
        {
            await _db.KeyDeleteAsync(UserKey(userId));
            return userId;
        }

        return null;
    }
}