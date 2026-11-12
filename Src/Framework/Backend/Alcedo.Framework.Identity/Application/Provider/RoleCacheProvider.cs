using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Framework.Identity.Application.Dto.Role;
using Alcedo.Framework.Identity.Application.IStore;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Identity.Infrastructure;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using MapsterMapper;
using Microsoft.Extensions.Options;

namespace Alcedo.Framework.Identity.Application.Provider;

public interface IRoleCacheProvider : IEntityCacheProvider<RoleCacheDto>
{
    Task<bool> CheckPermissionByRoleIdsAsync(IEnumerable<Guid> roleIds, string endPoint,
        CancellationToken ct = default);
}

[RegisterAs(typeof(IRoleCacheProvider))]
internal sealed class RoleCacheProvider : EntityCacheProvider<RoleAgg, RoleCacheDto>, IRoleCacheProvider
{
    private readonly IMasterDataRepositoryBase<RoleAgg> _roleRepository;
    private readonly IMapper _mapper;

    public RoleCacheProvider(
        IRoleCacheRedisStore cacheStore,
        IMasterDataRepositoryBase<RoleAgg> roleRepository,
        IMapper mapper,
        IOptions<RbacSchemeOptions> options)
        : base(cacheStore, TimeSpan.FromMinutes(options.Value.RoleCacheExpiryMinutes), roleRepository, mapper)
    {
        _roleRepository = roleRepository;
        _mapper = mapper;
    }

    public async Task<bool> CheckPermissionByRoleIdsAsync(IEnumerable<Guid> roleIds, string endPoint,
        CancellationToken ct = default)
    {
        var missingIds = new List<Guid>();

        foreach (var roleId in roleIds)
        {
            var cached = await CacheStore.GetByIdAsync(roleId);

            if (cached is null)
            {
                missingIds.Add(roleId);
                continue;
            }

            if (cached.IsEnabled &&
                cached.AllowedEndpoints.Contains(endPoint, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        if (!missingIds.Any())
        {
            return false;
        }

        var roles = await _roleRepository.GetListStrictByIdsAsync(missingIds, ct);
        var roleCaches = _mapper.Map<List<RoleCacheDto>>(roles);

        await Task.WhenAll(roleCaches.Select(c => SetAsync(c)));

        return roleCaches.Any(c =>
            c.IsEnabled && c.AllowedEndpoints.Contains(endPoint, StringComparer.OrdinalIgnoreCase));
    }
}