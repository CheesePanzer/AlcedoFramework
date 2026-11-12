using Alcedo.Framework.Notification.Application.Events;
using Alcedo.Framework.Notification.Infrastructure.Email;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Alcedo.Framework.Notification.Consumer;

public sealed class NotificationEmailSender : IConsumer<NotificationEmailSendEvent>
{
    private readonly EmailService _emailService;

    private readonly ILogger<NotificationEmailSender> _logger;

    public NotificationEmailSender(EmailService emailService, ILogger<NotificationEmailSender> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<NotificationEmailSendEvent> context)
    {
        _logger.LogDebug($"Message received: {context.Message.EventId} {context.Message.Title}");
        var msg = context.Message;
        await _emailService.SendAsync(msg.RecipientEmail, msg.RecipientName, msg.Title, msg.Body);
    }
}