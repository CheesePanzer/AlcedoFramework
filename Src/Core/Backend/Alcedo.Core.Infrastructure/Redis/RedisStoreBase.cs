using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Alcedo.Core.Infrastructure.Redis;

public abstract class RedisStoreBase<TKeySpace>
    where TKeySpace : RedisKeySpaceBase
{
    protected readonly IDatabase _db;
    protected readonly ILogger _logger;

    protected RedisStoreBase(TKeySpace keySpace, ILogger logger)
    {
        _db = keySpace.Db;
        _logger = logger;
    }

    protected Task DeleteValueAsync(string key)
    {
        return _db.KeyDeleteAsync(key);
    }

    protected Task KeyExpireAsync(string key, TimeSpan expiry)
    {
        return _db.KeyExpireAsync(key, expiry);
    }
}

public abstract class RedisJsonStoreBase<TKeySpace, TValue> : RedisStoreBase<TKeySpace>
    where TKeySpace : RedisKeySpaceBase
    where TValue : class
{
    protected RedisJsonStoreBase(TKeySpace keySpace, ILogger logger) : base(keySpace, logger)
    {
    }

    protected Task SetValueAsync(string key, TValue value, TimeSpan expiry)
    {
        var json = JsonSerializer.Serialize(value);
        return _db.StringSetAsync(key, json, expiry);
    }

    protected async Task<TValue?> GetValueAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        if (!value.HasValue)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TValue>(value!.ToByteArray());
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Could not deserialize {Type} from key {Key}", typeof(TValue).Name, key);
            throw;
        }
    }
}

public abstract class RedisSimpleValueStoreBase<TKeySpace, TValue> : RedisStoreBase<TKeySpace>
    where TKeySpace : RedisKeySpaceBase
{
    protected RedisSimpleValueStoreBase(TKeySpace keySpace, ILogger logger) : base(keySpace, logger)
    {
    }

    protected Task SetValueAsync(string key, TValue value, TimeSpan expiry)
    {
        RedisValue redisValue = value switch
        {
            string s => s,
            int i => i,
            long l => l,
            double d => d,
            bool b => b,
            Guid guid => guid.ToString(),
            decimal dec => dec.ToString(CultureInfo.InvariantCulture), // 使用不变文化特性转换为 string 存储
            _ => JsonSerializer.Serialize(value)
        };

        return _db.StringSetAsync(key, redisValue, expiry);
    }

    protected async Task<TValue?> GetValueAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        if (!value.HasValue)
        {
            return default;
        }

        try
        {
            if (typeof(TValue) == typeof(string))
            {
                return (TValue)(object)value.ToString();
            }
            if (typeof(TValue) == typeof(int))
            {
                return (TValue)(object)(int)value;
            }
            if (typeof(TValue) == typeof(Guid))
            {
                return (TValue)(object)Guid.Parse(value.ToString()!);
            }
            if (typeof(TValue) == typeof(decimal))
            {
                return (TValue)(object)decimal.Parse(value.ToString()!, CultureInfo.InvariantCulture);
            }
            
            return JsonSerializer.Deserialize<TValue>(value!.ToByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not convert or deserialize {Type} from key {Key}", typeof(TValue).Name, key);
            throw;
        }
    }
}