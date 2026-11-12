using Alcedo.Core.Abstractions;

namespace Alcedo.Framework.Notification.Infrastructure.Email;

public class SmtpOptions : AppSettingsBase
{
    [AppSettingsDemo("localhost", "SMTP server host. Use localhost with Papercut or MailHog for development.")]
    public string Host { get; init; } = "localhost";

    [AppSettingsDemo("25", "SMTP server port. Typically 25 (local), 587 (STARTTLS), or 465 (SSL).")]
    public int Port { get; init; } = 25;

    [AppSettingsDemo("", "SMTP authentication username. Leave empty for local test servers.")]
    public string Username { get; init; } = string.Empty;

    [AppSettingsDemo("", "SMTP authentication password or API key. Leave empty for local test servers.")]
    public string Password { get; init; } = string.Empty;

    [AppSettingsDemo("false", "Whether to use SSL/TLS. Set to true for production SMTP providers.")]
    public bool UseSsl { get; init; }

    [AppSettingsDemo("noreply@alcedo.com", "Sender email address shown in the From field.")]
    public string FromEmail { get; init; } = string.Empty;

    [AppSettingsDemo("Alcedo", "Sender display name shown in the From field.")]
    public string FromName { get; init; } = string.Empty;
}