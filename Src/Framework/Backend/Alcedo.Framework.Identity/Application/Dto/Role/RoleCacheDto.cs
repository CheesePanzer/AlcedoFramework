using Alcedo.Core.Application.Dto;

namespace Alcedo.Framework.Identity.Application.Dto.Role;

public sealed record RoleCacheDto : EntityCacheDtoBase
{
    public bool IsEnabled { get; init; }

    public IReadOnlyList<string> AllowedEndpoints { get; init; } = new List<string>();
}