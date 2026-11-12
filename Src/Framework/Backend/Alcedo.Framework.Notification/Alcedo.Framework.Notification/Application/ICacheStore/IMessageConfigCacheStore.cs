using Alcedo.Framework.Notification.Application.Dto;

namespace Alcedo.Framework.Notification.Application.ICacheStore;

public interface IMessageConfigCacheStore
{
    Task SetAsync(MessageConfigCacheDto dto, TimeSpan expiry);

    Task<MessageConfigCacheDto?> GetByKeyAsync(string code);

    Task DeleteByKeyAsync(string code);
}