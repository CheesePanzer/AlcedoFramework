using Alcedo.Core.Application.SearchQuery;

namespace Alcedo.Framework.Identity.Application.SearchQuery;

public sealed record LoginLogSearchQuery : EntitySearchQueryBase
{
    public Guid? UserId { get; set; }
    public string? LoginName { get; set; }
    public string? IpAddress { get; set; }
    public bool? IsSuccess { get; set; }
}