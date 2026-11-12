using Alcedo.Core.Application;
using Alcedo.Core.Infrastructure.Consumer;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Domain.Entity;

namespace Alcedo.Framework.Identity.Consumer.User;

public sealed class UserCacheRefreshConsumer : EntityCacheRefreshConsumerBase<UserCacheRefreshEvent, UserCacheDto>
{
    public UserCacheRefreshConsumer(IEntityCacheProvider<UserCacheDto> cacheProvider) : base(cacheProvider)
    {
    }
}