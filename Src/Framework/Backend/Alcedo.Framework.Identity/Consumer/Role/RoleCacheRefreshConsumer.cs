using Alcedo.Core.Infrastructure.Consumer;
using Alcedo.Framework.Identity.Application.Dto.Role;
using Alcedo.Framework.Identity.Application.Provider;
using Alcedo.Framework.Identity.Domain.Entity;

namespace Alcedo.Framework.Identity.Consumer.Role;

public sealed class RoleCacheRefreshConsumer : EntityCacheRefreshConsumerBase<RoleCacheRefreshEvent, RoleCacheDto>
{
    public RoleCacheRefreshConsumer(IRoleCacheProvider cacheProvider) : base(cacheProvider)
    {
    }
}