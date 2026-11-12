using Alcedo.Core.Application.SearchQuery;

namespace Alcedo.Framework.Notification.Application.SearchQuery;

public record MessageErrorSearchQuery : EntitySearchQueryBase
{
    public string? MessageTypeCode { get; set; }
}