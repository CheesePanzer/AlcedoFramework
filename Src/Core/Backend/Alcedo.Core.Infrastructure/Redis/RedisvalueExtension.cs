using StackExchange.Redis;

namespace Alcedo.Core.Infrastructure.Redis;

public static class RedisValueExtensions
{
    public static byte[]? ToByteArray(this RedisValue value)
    {
        if (value.IsNull)
        {
            return null;
        }

        return (byte[])value!;
    }
}