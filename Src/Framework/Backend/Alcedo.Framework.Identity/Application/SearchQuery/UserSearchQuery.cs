using Alcedo.Framework.Shared.Application.SearchQuery;

namespace Alcedo.Framework.Identity.Application.SearchQuery;

public sealed record UserSearchQuery : MasterDataSearchQueryBase
{
    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? Contact { get; set; }
}