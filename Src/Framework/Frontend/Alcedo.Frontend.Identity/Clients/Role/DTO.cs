using Alcedo.Frontend.Abstraction.Attributes;
using Alcedo.Frontend.Abstraction.Command;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;

namespace Alcedo.Frontend.Identity.Clients.Role;

[AutoTableLink(nameof(Code), "/identity/roles/{Id}")]
public sealed record RoleDto : MasterDataDtoBase
{
    public string? Description { get; set; }

    public IReadOnlyList<string> AllowedEndpoints { get; set; } = new List<string>();
}

public sealed record RoleSearchQuery : MasterDataSearchQueryBase;

public sealed record RoleCreateCommand : MasterDataCreateCommandBase
{
    public string? Description { get; set; }
}

public sealed record RoleUpdateCommand : MasterDataUpdateCommandBase
{
    public string? Description { get; set; }
}