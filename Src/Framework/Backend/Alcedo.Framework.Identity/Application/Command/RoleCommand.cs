using Alcedo.Framework.Shared.Application.Command;

namespace Alcedo.Framework.Identity.Application.Command;

public sealed record RoleCreateCommand : MasterDataCreateCommandBase
{
    public string? Description { get; init; }
}

public sealed record RoleUpdateCommand : MasterDataUpdateCommandBase
{
    public string? Description { get; init; }
}