namespace Alcedo.Framework.Notification.Contracts;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MessageTypeAttribute : Attribute
{
    public string? Code { get; init; }
    public string? Name { get; init; }

    public string? Description { get; init; }

    public MessageTypeAttribute(string? code, string? name = null, string? description = null)
    {
        Code = code;
        Name = name;
        Description = description;
    }
}

public enum NotificationChannel
{
    Email,
    Sms,
    InApp
}