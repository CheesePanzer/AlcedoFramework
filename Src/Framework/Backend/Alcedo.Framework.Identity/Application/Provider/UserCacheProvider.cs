using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Domain.IRepositoryBase;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Identity.Infrastructure;
using MapsterMapper;
using Microsoft.Extensions.Options;

namespace Alcedo.Framework.Identity.Application.Provider;

[RegisterAs(typeof(IEntityCacheProvider<UserCacheDto>))]
internal sealed class UserCacheProvider(
    IEntityCacheStore<UserCacheDto> cacheStore,
    IEntityStoreBase<UserAgg> dbStore,
    IMapper mapper,
    IOptions<SecurityOptions> options)
    : EntityCacheProvider<UserAgg, UserCacheDto>(
        cacheStore,
        TimeSpan.FromMinutes(options.Value.RefreshTokenValidMinutes),
        dbStore,
        mapper);