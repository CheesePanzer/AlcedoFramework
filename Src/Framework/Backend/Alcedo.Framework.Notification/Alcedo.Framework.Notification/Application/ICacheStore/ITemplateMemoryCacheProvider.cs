using Alcedo.Framework.Notification.Contracts;
using Scriban;

namespace Alcedo.Framework.Notification.Application.ICacheStore;

public record TemplateCacheItem(Guid Id, bool IsActive, NotificationChannel Channel, Template? TitleTemplate, Template? BodyTemplate, string? ExternalTemplateId);

public interface ITemplateMemoryCacheProvider
{
    Task<TemplateCacheItem?> GetOrFallbackDbAsync(Guid templateId);
    Task<TemplateCacheItem?> RefreshFromDbAsync(Guid templateId);
}