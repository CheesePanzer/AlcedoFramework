using Alcedo.Framework.Notification.Application.Provider;
using Alcedo.Framework.Notification.Domain.Entity;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Alcedo.Framework.Notification.Consumer;

public class MessageConfigCacheRefreshConsumer : IConsumer<MessageConfigCacheRefreshEvent>
{
    private readonly IMessageConfigCacheProvider _messageConfigCacheProvider;
    
    private readonly ILogger<MessageConfigCacheRefreshConsumer> _logger;

    public MessageConfigCacheRefreshConsumer(
        IMessageConfigCacheProvider messageConfigCacheProvider, ILogger<MessageConfigCacheRefreshConsumer> logger)
    {
        _messageConfigCacheProvider = messageConfigCacheProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MessageConfigCacheRefreshEvent> context)
    {
        var dto = await _messageConfigCacheProvider.RefreshFromDbAsync(context.Message.AggregateId);

        if (dto is null)
        {
            _logger.LogError($"Message Config Cache with Id: {context.Message.AggregateId} not found");
        }
    }
}