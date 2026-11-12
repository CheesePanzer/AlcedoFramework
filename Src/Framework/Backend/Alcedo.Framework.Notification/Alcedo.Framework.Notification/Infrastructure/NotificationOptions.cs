using Alcedo.Core.Abstractions;

namespace Alcedo.Framework.Notification.Infrastructure;

public sealed class NotificationOptions : AppSettingsBase
{
    [AppSettingsDemo("120", "Expiry minutes of Message template cache")]
    public int MessageCacheGlobalExpiryMins { get; init; } = 120;
}