using Alcedo.Core.Application.Dto;

namespace Alcedo.Framework.Identity.Application.Dto.User;

public sealed record UserCacheDto : EntityCacheDtoBase
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string LoginName { get; init; }

    public required IReadOnlyList<Guid> RoleIds { get; init; }

    public required bool CanAccessContext { get; init; }

    public required bool IsRootUser { get; init; }

    public required Guid? CurrentSession { get; init; }
}