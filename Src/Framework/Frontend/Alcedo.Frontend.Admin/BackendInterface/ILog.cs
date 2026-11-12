using Alcedo.Frontend.Abstraction;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;

namespace Alcedo.Frontend.Admin.BackendInterface;

public sealed record AccessLogDto : EntityDtoBase
{
    public string? TraceId { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public string? UserAgent { get; set; }

    public string Method { get; set; } = string.Empty;

    public string? Area { get; set; }

    public string Controller { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public DateTimeOffset RequestedAt { get; set; }

    public int? StatusCode { get; set; }

    public int? ExecutionMs { get; set; }

    public string? Exception { get; set; }
}

public sealed record AccessLogSearchQuery : EntitySearchQueryBase
{
    public string? TraceId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? Method { get; set; }

    public string? Area { get; set; }

    public string? Controller { get; set; }

    public string? Action { get; set; }

    public string? Url { get; set; }
}

[ApiClient(Area = "admin", Controller = "logs", Version = 1)]
public interface ILog
{
    [HttpGet("accesslog")]
    Task<PagedResult<AccessLogDto>> GetListAsync([ToQuery] AccessLogSearchQuery query, [ToQuery] SortQuery sortQuery,
        [ToQuery] PaginationQuery paginationQuery, CancellationToken ct = default);
}