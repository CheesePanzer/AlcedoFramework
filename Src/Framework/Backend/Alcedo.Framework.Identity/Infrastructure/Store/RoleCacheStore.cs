using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure.Redis;
using Alcedo.Framework.Identity.Application.Dto.Role;
using Alcedo.Framework.Identity.Application.IStore;
using Alcedo.Framework.Identity.Infrastructure.RedisKeySpace;
using Microsoft.Extensions.Logging;

namespace Alcedo.Framework.Identity.Infrastructure.Store;

[RegisterAs(typeof(IRoleCacheRedisStore))]
internal sealed class RoleCacheRedisStore : EntityCacheRedisStore<RoleCacheRedisKeySpace, RoleCacheDto>,
    IRoleCacheRedisStore
{
    public RoleCacheRedisStore(RoleCacheRedisKeySpace keySpace, ILogger<RoleCacheRedisStore> logger)
        : base(keySpace, logger)
    {
    }

    public async Task<EndpointPermissionCheckResult> CheckEndpointPermissionAsync(
        IEnumerable<Guid> roleIds,
        string endpoint)
    {
        var missingRoleIds = new List<Guid>();

        foreach (var roleId in roleIds)
        {
            var cachedRole = await GetByIdAsync(roleId);

            if (cachedRole is null)
            {
                missingRoleIds.Add(roleId);
                continue;
            }

            if (!cachedRole.IsEnabled)
            {
                continue;
            }

            if (cachedRole.AllowedEndpoints.Contains(endpoint, StringComparer.OrdinalIgnoreCase))
            {
                return new EndpointPermissionCheckResult
                {
                    HasPermission = true
                };
            }
        }

        return new EndpointPermissionCheckResult
        {
            HasPermission = false,
            MissingRoleCacheIds = missingRoleIds
        };
    }
}