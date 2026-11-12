using Alcedo.Core.Application;
using Alcedo.Framework.Identity.Application.Dto.Role;

namespace Alcedo.Framework.Identity.Application.IStore;

public interface IRoleCacheRedisStore : IEntityCacheStore<RoleCacheDto>
{
    Task<EndpointPermissionCheckResult> CheckEndpointPermissionAsync(
        IEnumerable<Guid> roleIds,
        string endpoint);
}

public sealed class EndpointPermissionCheckResult
{
    public bool HasPermission { get; init; }

    public IReadOnlyList<Guid> MissingRoleCacheIds { get; init; }
        = Array.Empty<Guid>();
}