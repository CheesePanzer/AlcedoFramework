using System.Text.Json;
using Alcedo.Core.Application;

namespace Alcedo.Framework.Notification.Contracts;

public sealed record NotificationDispatchEvent : ApplicationEventBase
{
    public string MessageTypeCode { get; init; }
    public string PayloadJson { get; init; }
    public RecipientContext Recipient { get; init; }

    public NotificationDispatchEvent(string messageTypeCode, string payloadJson, RecipientContext recipient)
    {
        MessageTypeCode = messageTypeCode;
        PayloadJson = payloadJson;
        Recipient = recipient;
    }

    public static NotificationDispatchEvent Create<TPayload>(TPayload payload,
        RecipientContext recipient)
        where TPayload : INotificationMessage
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        };
        var meta = MessageTypeMetadataCenter.GetMetadata<TPayload>();
        var json = JsonSerializer.Serialize(payload, typeof(TPayload), serializerOptions);
        return new NotificationDispatchEvent(meta.Code, json, recipient);
    }
}

public sealed record RecipientContext
{
    public string RecipientName { get; init; }
    public Guid RecipientId { get; init; }
    public string? RecipientType { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }

    public bool CanInApp => RecipientId != Guid.Empty && !string.IsNullOrWhiteSpace(RecipientType);

    public bool CanEmail => !string.IsNullOrWhiteSpace(Email);

    public bool CanPhone => !string.IsNullOrWhiteSpace(Phone);

    public RecipientContext(string recipientName, Guid recipientId, string? recipientType = null, string? email = null,
        string? phone = null)
    {
        RecipientName = recipientName;
        RecipientId = recipientId;
        RecipientType = recipientType;
        Email = email;
        Phone = phone;
    }
}