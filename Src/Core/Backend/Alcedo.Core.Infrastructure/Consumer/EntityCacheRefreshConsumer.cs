using Alcedo.Core.Application;
using Alcedo.Core.Application.Dto;
using Alcedo.Core.Domain.Event;
using MassTransit;

namespace Alcedo.Core.Infrastructure.Consumer;

public abstract class EntityCacheRefreshConsumerBase<TEvent, TDto> : IConsumer<TEvent>
    where TEvent : EntityCacheRefreshEvent
    where TDto : EntityCacheDtoBase
{
    private readonly IEntityCacheProvider<TDto> _cacheProvider;

    protected EntityCacheRefreshConsumerBase(IEntityCacheProvider<TDto> cacheProvider)
    {
        _cacheProvider = cacheProvider;
    }

    public virtual async Task Consume(ConsumeContext<TEvent> context)
    {
        await _cacheProvider.RefreshFromDbAsync(context.Message.AggregateId);
    }
}