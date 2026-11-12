using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.AppException;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.IRepositoryBase;
using Alcedo.Framework.Notification.Application.Dto;
using Alcedo.Framework.Notification.Application.Events;
using Alcedo.Framework.Notification.Application.Helper;
using Alcedo.Framework.Notification.Application.ICacheStore;
using Alcedo.Framework.Notification.Application.Provider;
using Alcedo.Framework.Notification.Contracts;
using Alcedo.Framework.Notification.Domain.Entity;
using Microsoft.Extensions.Logging;
using Scriban.Syntax;

namespace Alcedo.Framework.Notification.Application.Service.MessageDispatch;

public interface IMessageDispatchApplicationService
{
    Task HandleNotificationDispatchEventAsync(NotificationDispatchEvent @event);
}

[RegisterAs(typeof(IMessageDispatchApplicationService))]
internal sealed class MessageDispatchApplicationService : IMessageDispatchApplicationService
{
    private readonly ITemplateMemoryCacheProvider _templateCacheProvider;

    private readonly IMessageConfigCacheProvider _configCacheProvider;

    private readonly IUnitOfWorkExecutor _executor;

    private readonly ILogger<MessageDispatchApplicationService> _logger;
    
    private readonly IEntityStoreBase<MessageErrorEntity> _store;

    public MessageDispatchApplicationService(
        ITemplateMemoryCacheProvider templateCacheProvider,
        ILogger<MessageDispatchApplicationService> logger, IUnitOfWorkExecutor executor,
        IMessageConfigCacheProvider configCacheProvider, IEntityStoreBase<MessageErrorEntity> store)
    {
        _templateCacheProvider = templateCacheProvider;
        _logger = logger;
        _executor = executor;
        _configCacheProvider = configCacheProvider;
        _store = store;
    }
    
    private Task AddMessageError(NotificationDispatchEvent @event, string message)
    => _executor.ExecuteAsync(() =>
        {
            _store.Add(new MessageErrorEntity(@event, message));
        });
    
    private async Task<MessageConfigCacheDto?> GetConfigAsync(
        NotificationDispatchEvent @event)
    {
        var config =
            await _configCacheProvider
                .GetOrFallbackDbAsync(@event.MessageTypeCode);

        if (config is null)
        {
            await AddMessageError(
                @event,
                "Message config not found");

            return null;
        }

        if (config.ShouldSkip)
        {
            _logger.LogInformation(
                "Notification skipped");

            return null;
        }

        if (!config.IsActive)
        {
            await AddMessageError(
                @event,
                "Message not active");

            return null;
        }

        if (!config.TemplateIds.Any())
        {
            await AddMessageError(
                @event,
                "No templates");

            return null;
        }

        return config;
    }
    
    private async Task<TemplateCacheItem?> GetTemplateAsync(
        Guid templateId,
        NotificationDispatchEvent @event)
    {
        var template =
            await _templateCacheProvider
                .GetOrFallbackDbAsync(templateId);

        if (template is null)
        {
            await AddMessageError(
                @event,
                $"Template {templateId} not found");

            return null;
        }

        if (!template.IsActive)
        {
            await AddMessageError(
                @event,
                $"Template {templateId} inactive");

            return null;
        }

        return template;
    }
    
    // Any template processing failure of any channel will abort the whole notification dispatch.
    public async Task HandleNotificationDispatchEventAsync(NotificationDispatchEvent @event)
    {
        var config = await GetConfigAsync(@event);

        if (config is null)
        {
            return;
        }

        ICollection<ApplicationEventBase> events = new List<ApplicationEventBase>();

        foreach (var templateId in config.TemplateIds)
        {
            var template = await GetTemplateAsync(templateId, @event);

            if (template is null)
            {
                return;
            }

            var sendEvent = await BuildEventAsync(template, @event);

            if (sendEvent is null)
            {
                return;
            }
            
            events.Add(sendEvent);
        }

        if (events.Count > 0)
        {
            await _executor.ExecuteAsync(() =>
            {
                _executor.AddApplicationEvents(events);
            });
        }
    }
    
    private async Task<ApplicationEventBase?>
        BuildEventAsync(
            TemplateCacheItem template,
            NotificationDispatchEvent @event)
    {
        return template.Channel switch
        {
            NotificationChannel.Email =>
                await BuildEmailEventAsync(template, @event),

            NotificationChannel.Sms =>
                null,

            NotificationChannel.InApp =>
                null,

            _ => null
        };
    }

    private async Task<ApplicationEventBase?> BuildEmailEventAsync(TemplateCacheItem template, NotificationDispatchEvent @event)
    {
        if (!@event.Recipient.CanEmail)
        {
            await AddMessageError(
                @event,
                $"Recipient {@event.Recipient.RecipientId} : {@event.Recipient.RecipientName} email is empty, but need a email notification");

            return null;
        }

        if (template.TitleTemplate is null)
        {
            await AddMessageError(
                @event,
                $"Message Dispatch Event {@event.EventId} with Template Definition Id = {template.Id} has empty title");

            return null;
        }
                    
        try
        {
            var result =
                await MessageTemplateRenderHelper.RenderAsync(
                    template.TitleTemplate,
                    template.BodyTemplate,
                    @event.PayloadJson);

            return new NotificationEmailSendEvent(
                result.title,
                result.body,
                @event.Recipient.Email!,
                @event.Recipient.RecipientName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message Template {TemplateId} Render Failed", template.Id);
            await AddMessageError(
                @event,
                $"Message Template Render Failed: {ex.Message}");
            return null;
        }
    }
}