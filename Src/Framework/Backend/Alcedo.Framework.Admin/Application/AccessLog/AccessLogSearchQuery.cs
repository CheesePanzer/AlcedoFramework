using Alcedo.Core.Application.SearchQuery;

namespace Alcedo.Framework.Admin.Application.AccessLog;

public sealed record AccessLogSearchQuery : EntitySearchQueryBase
{
    public string? TraceId { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? Method { get; init; }

    public string? Area { get; init; }

    public string? Controller { get; init; }

    public string? Action { get; init; }

    public string? Url { get; init; }
}