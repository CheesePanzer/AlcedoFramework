using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure;
using Alcedo.Core.Infrastructure.Redis;
using Alcedo.Framework.Notification.Application.Dto;
using Alcedo.Framework.Notification.Application.ICacheStore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Alcedo.Framework.Notification.Infrastructure.Store;

internal sealed class MessageConfigCacheRedisKeySpace : RedisKeySpaceBase
{
    public MessageConfigCacheRedisKeySpace(IConnectionMultiplexer multiplexer, IOptions<RedisOptions> options,
        AlcedoModulesRegistry registry) : base(multiplexer, options, registry)
    {
    }
}

[RegisterAs(typeof(IMessageConfigCacheStore))]
internal class MessageConfigRedisStore : RedisJsonStoreBase<MessageConfigCacheRedisKeySpace, MessageConfigCacheDto>,
    IMessageConfigCacheStore
{
    public MessageConfigRedisStore(MessageConfigCacheRedisKeySpace keySpace, ILogger<MessageConfigRedisStore> logger) :
        base(keySpace, logger)
    {
    }

    public Task SetAsync(MessageConfigCacheDto dto, TimeSpan expiry)
        => SetValueAsync(dto.MessageTypeCode, dto, expiry);

    public Task<MessageConfigCacheDto?> GetByKeyAsync(string code)
        => GetValueAsync(code);

    public Task DeleteByKeyAsync(string code)
        => DeleteValueAsync(code);
}