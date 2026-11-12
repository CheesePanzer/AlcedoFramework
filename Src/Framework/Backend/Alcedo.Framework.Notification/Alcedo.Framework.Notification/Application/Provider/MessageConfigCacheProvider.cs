using Alcedo.Core.Abstractions;
using Alcedo.Core.Domain.DomException;
using Alcedo.Framework.Notification.Application.Dto;
using Alcedo.Framework.Notification.Application.ICacheStore;
using Alcedo.Framework.Notification.Domain.Entity;
using Alcedo.Framework.Notification.Domain.IRepository;
using Alcedo.Framework.Notification.Infrastructure;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using MapsterMapper;
using Microsoft.Extensions.Options;

namespace Alcedo.Framework.Notification.Application.Provider;

public interface IMessageConfigCacheProvider
{
    Task<MessageConfigCacheDto?> GetOrFallbackDbAsync(string code, CancellationToken ct = default);

    Task<MessageConfigCacheDto?> RefreshFromDbAsync(Guid id, TimeSpan? expiry = null, CancellationToken ct = default);
}

[RegisterAs(typeof(IMessageConfigCacheProvider))]
internal sealed class MessageConfigCacheProvider : IMessageConfigCacheProvider
{
    private readonly IMessageConfigCacheStore _cacheStore;
    private readonly IMessageConfigRepository _dbStore;
    private readonly IMapper _mapper;
    private readonly TimeSpan _defaultExpiry;

    public MessageConfigCacheProvider(IMessageConfigCacheStore cacheStore,
        IMessageConfigRepository dbStore, IMapper mapper, IOptions<NotificationOptions> options)
    {
        _cacheStore = cacheStore;
        _dbStore = dbStore;
        _mapper = mapper;
        _defaultExpiry = TimeSpan.FromMinutes(options.Value.MessageCacheGlobalExpiryMins);
    }

    private async Task<MessageConfigCacheDto?> FetchFromDbAsync(string code, CancellationToken ct)
    {
        var agg = await _dbStore.GetByMsgTypeCodeNoTrackingAsync(code, ct);
        if (agg is null) return null;
        return _mapper.Map<MessageConfigCacheDto>(agg);
    }

    private async Task<MessageConfigCacheDto?> FetchFromDbAsync(Guid id, CancellationToken ct)
    {
        var agg = await _dbStore.GetByIdNoTrackingAsync(id, ct);
        if (agg is null) return null;
        return _mapper.Map<MessageConfigCacheDto>(agg);
    }

    public async Task<MessageConfigCacheDto?> GetOrFallbackDbAsync(string code, CancellationToken ct = default)
    {
        var cached = await _cacheStore.GetByKeyAsync(code);
        if (cached is not null)
        {
            return cached;
        }

        return await RefreshFromDbAsync(code, ct: ct);
    }

    public async Task<MessageConfigCacheDto?> RefreshFromDbAsync(string code, TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var dto = await FetchFromDbAsync(code, ct);
        if (dto is null) return null;
        await _cacheStore.SetAsync(dto, expiry ?? _defaultExpiry);
        return dto;
    }

    public async Task<MessageConfigCacheDto?> RefreshFromDbAsync(Guid id, TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var dto = await FetchFromDbAsync(id, ct);
        if (dto is null) return null;
        await _cacheStore.SetAsync(dto, expiry ?? _defaultExpiry);
        return dto;
    }
}