using Alcedo.Core.Application;
using Alcedo.Core.Infrastructure.Consumer;
using Alcedo.Framework.Notification.Application.Dto;
using Alcedo.Framework.Notification.Application.ICacheStore;
using Alcedo.Framework.Notification.Domain.Entity;
using MassTransit;

namespace Alcedo.Framework.Notification.Consumer;

public class
    MessageTemplateCacheRefreshConsumer : IConsumer<MessageTemplateCacheRefreshEvent>
{
    private readonly ITemplateMemoryCacheProvider _cacheProvider;

    public MessageTemplateCacheRefreshConsumer(ITemplateMemoryCacheProvider cacheProvider)
    {
        _cacheProvider = cacheProvider;
    }

    public Task Consume(ConsumeContext<MessageTemplateCacheRefreshEvent> context)
        => _cacheProvider.RefreshFromDbAsync(context.Message.AggregateId);
}