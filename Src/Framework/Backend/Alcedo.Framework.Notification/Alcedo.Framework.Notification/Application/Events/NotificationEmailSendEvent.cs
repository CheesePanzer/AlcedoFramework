using Alcedo.Core.Application;
using Alcedo.Framework.Notification.Contracts;

namespace Alcedo.Framework.Notification.Application.Events;

public sealed record NotificationEmailSendEvent : ApplicationEventBase
{
    public string Title { get; init; }
    public string Body { get; init; }
    public string RecipientEmail { get; init; }
    public string RecipientName { get; init; }

    public NotificationEmailSendEvent(string title, string body, string recipientEmail, string recipientName)
    {
        Title = title;
        Body = body;
        RecipientEmail = recipientEmail;
        RecipientName = recipientName;
    }
}