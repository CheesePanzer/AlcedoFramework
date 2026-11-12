using Alcedo.Framework.Notification.Application.Service.MessageDispatch;
using Alcedo.Framework.Notification.Contracts;
using MassTransit;

namespace Alcedo.Framework.Notification.Consumer;

public sealed class NotificationDispatchConsumer : IConsumer<NotificationDispatchEvent>
{
    private readonly IMessageDispatchApplicationService _service;

    public NotificationDispatchConsumer(IMessageDispatchApplicationService service)
    {
        _service = service;
    }

    public Task Consume(ConsumeContext<NotificationDispatchEvent> context)
        => _service.HandleNotificationDispatchEventAsync(context.Message);
}