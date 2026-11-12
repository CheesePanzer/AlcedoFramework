using Alcedo.Core.Application.Dto;

namespace Alcedo.Framework.Admin.Application.AccessLog;

public sealed record AccessLogDto : EntityDtoBase
{
    public required string? TraceId { get; set; }

    public required string IpAddress { get; set; } = string.Empty;

    public required string? UserAgent { get; set; }

    public required string Method { get; set; } = string.Empty;

    public required string? Area { get; set; }

    public required string Controller { get; set; } = string.Empty;

    public required string Action { get; set; } = string.Empty;

    public required string Url { get; set; } = string.Empty;

    public required DateTimeOffset RequestedAt { get; set; }

    public required int? StatusCode { get; set; }

    public required int? ExecutionMs { get; set; }

    public required string? Exception { get; set; }
}