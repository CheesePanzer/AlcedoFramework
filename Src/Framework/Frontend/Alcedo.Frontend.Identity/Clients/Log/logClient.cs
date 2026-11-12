using Alcedo.Frontend.Abstraction;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;

namespace Alcedo.Frontend.Identity.Clients.Log;

public sealed record LoginLogSearchQuery : EntitySearchQueryBase
{
    public Guid? UserId { get; set; }
    public string? LoginName { get; set; }
    public string? IpAddress { get; set; }
    public bool? IsSuccess { get; set; }
}

public sealed record LoginLogDto : EntityDtoBase
{
    public required Guid? UserId { get; init; }
    public required string LoginName { get; init; }
    public required string IpAddress { get; init; }
    public required bool IsSuccess { get; init; }
    public required Guid? SessionId { get; init; }
    public required string? FailReason { get; init; }
    public required DateTimeOffset LoginAt { get; init; }
}

[ApiClient(Area = "identity", Controller = "logs", Version = 1)]
public interface ILogClient
{
    [HttpGet("loginlog")]
    Task<PagedResult<LoginLogDto>> GetListAsync([ToQuery] LoginLogSearchQuery query, [ToQuery] SortQuery sortQuery,
        [ToQuery] PaginationQuery paginationQuery, CancellationToken ct = default);
}