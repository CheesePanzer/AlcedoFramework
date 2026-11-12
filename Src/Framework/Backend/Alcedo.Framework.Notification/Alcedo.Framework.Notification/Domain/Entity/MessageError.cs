using Alcedo.Core.Domain.Entity;
using Alcedo.Framework.Notification.Contracts;

namespace Alcedo.Framework.Notification.Domain.Entity;

public sealed class MessageErrorEntity : EntityBase
{
    public string MessageTypeCode { get; private set; }
    
    public string PayloadJson { get; private set; }
    
    public string RecipientName { get; private set; }
    
    public Guid RecipientId { get; private set; }
    
    public string? RecipientType { get; private set; }
    
    public string? Email { get; private set; }
    
    public string? Phone { get; private set; }
    
    public string ErrorMsg { get; private set; }
    
    public DateTimeOffset FailedAt { get; private set; }

    private MessageErrorEntity()
    {
        
    }
    
    public MessageErrorEntity(
        NotificationDispatchEvent evt,
        string errorMsg)
    {
        MessageTypeCode = evt.MessageTypeCode;
        PayloadJson = evt.PayloadJson;
        RecipientName = evt.Recipient.RecipientName;
        RecipientId = evt.Recipient.RecipientId;
        RecipientType = evt.Recipient.RecipientType;
        Email = evt.Recipient.Email;
        Phone = evt.Recipient.Phone;
        FailedAt = DateTimeOffset.UtcNow;
        ErrorMsg = errorMsg;
    }
}