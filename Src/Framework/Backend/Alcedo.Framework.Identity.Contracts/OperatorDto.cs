using Alcedo.Framework.Shared.Application.Dto;

namespace Alcedo.Framework.Identity.Contracts;

public record OperatorDtoWithRoles : OperatorDto
{
    public required IReadOnlyCollection<Guid> RoleIds { get; init; } = Array.Empty<Guid>();

    public required bool IsRootUser { get; init; }
}