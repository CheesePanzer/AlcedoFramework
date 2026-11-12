using Alcedo.Framework.Shared.Application.Dto;

namespace Alcedo.Framework.Identity.Application.Dto.Role;

public sealed record RoleDto : MasterDataDtoBase
{
    public required string? Description { get; init; }

    public IReadOnlyList<string> AllowedEndpoints { get; init; } = new List<string>();
}