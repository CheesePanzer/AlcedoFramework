using System.Collections.Concurrent;
using Alcedo.Core.Domain.DomException;
using Alcedo.Framework.Notification.Application.ICacheStore;
using Alcedo.Framework.Notification.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using Microsoft.Extensions.Logging;
using Scriban;
using Serilog.Events;

namespace Alcedo.Framework.Notification.Infrastructure;

public class TemplateMemoryCacheProvider : ITemplateMemoryCacheProvider
{
    private static readonly ConcurrentDictionary<Guid, TemplateCacheItem> _templateCache = new();
    
    private readonly IMasterDataRepositoryBase<MessageTemplateAggRoot> _repository;
    
    private readonly ILogger<TemplateMemoryCacheProvider> _logger;

    public TemplateMemoryCacheProvider(IMasterDataRepositoryBase<MessageTemplateAggRoot> repository, ILogger<TemplateMemoryCacheProvider> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<TemplateCacheItem?> GetOrFallbackDbAsync(Guid templateId)
    {
        if (_templateCache.TryGetValue(templateId, out TemplateCacheItem? item))
        {
            _logger.LogDebug("Cache Hit for message template {TemplateId}", templateId);
            return item;
        }
        
        return await RefreshFromDbAsync(templateId);
    }

    public async Task<TemplateCacheItem?> RefreshFromDbAsync(Guid templateId)
    {
        _logger.LogInformation("Refreshing message template {TemplateId}", templateId);
        
        var templateAggRoot = await _repository.GetByIdAsync(templateId);

        if (templateAggRoot is null)
        {
            return null;
        }

        Template? titleTemplateCompiled = templateAggRoot.TitleTemplate is not null 
            ? Template.Parse(templateAggRoot.TitleTemplate) 
            : null;
        
        Template? bodyTemplateCompiled = templateAggRoot.BodyTemplate is not null 
            ? Template.Parse(templateAggRoot.BodyTemplate) 
            : null;

        var cacheItem = new TemplateCacheItem(
            templateAggRoot.Id,
            templateAggRoot.IsActive, 
            templateAggRoot.Channel,
            titleTemplateCompiled, 
            bodyTemplateCompiled, 
            templateAggRoot.ExternalTemplateId);

        _templateCache[templateId] = cacheItem;

        return cacheItem;
    }
}