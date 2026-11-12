namespace Alcedo.Framework.Notification.Application.SearchQuery;

public sealed record MessageTypeSearchQuery
{
    public string? Code { get; init; }

    public string? Name { get; init; }
}