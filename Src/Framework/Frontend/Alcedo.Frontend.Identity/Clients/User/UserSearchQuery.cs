using Alcedo.Frontend.Abstraction.SearchQuery;

namespace Alcedo.Frontend.Identity.Clients.User;

public sealed record UserSearchQuery : MasterDataSearchQueryBase
{
    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? Contact { get; set; }
}