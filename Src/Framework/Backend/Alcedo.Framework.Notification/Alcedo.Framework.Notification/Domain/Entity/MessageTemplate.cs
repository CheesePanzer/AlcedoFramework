using Alcedo.Core.Domain.Event;
using Alcedo.Framework.Notification.Contracts;
using Alcedo.Framework.Shared.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.Notification.Domain.Entity;

public sealed class MessageTemplateAggRoot : MasterDataAggregateRootBase
{
    public NotificationChannel Channel { get; private set; }

    public string? TitleTemplate { get; private set; }

    public string? BodyTemplate { get; private set; }

    public string? ExternalTemplateId { get; private set; }

    public bool IsActive => Status == MasterDataStates.Enabled;

#pragma warning disable CS8618
    private MessageTemplateAggRoot()
    {
    }
#pragma warning restore CS8618

    public MessageTemplateAggRoot(
        string code,
        string name,
        NotificationChannel channel,
        string? titleTemplate,
        string? bodyTemplate,
        string? externalTemplateId,
        OperatorRecord creator) : base(code, name, creator)
    {
        Channel = channel;

        TitleTemplate = titleTemplate;

        BodyTemplate = bodyTemplate;

        ExternalTemplateId = externalTemplateId;
    }

    public void Update(
        string name,
        NotificationChannel channel,
        string? titleTemplate,
        string? htmlTemplate,
        string? externalTemplateId,
        OperatorRecord updater)
    {
        base.Update(name, updater);

        Channel = channel;

        TitleTemplate = titleTemplate;

        BodyTemplate = htmlTemplate;

        ExternalTemplateId = externalTemplateId;

       TriggerCacheRefresh();
    }

    private void TriggerCacheRefresh()
    {
        AddEvent(new MessageTemplateCacheRefreshEvent(Id)); // publish refresh cache event
    }

    public override void Enable(OperatorRecord oper)
    {
        base.Enable(oper);
        TriggerCacheRefresh();
    }

    public override void Disable(OperatorRecord oper)
    {
        base.Disable(oper);
        TriggerCacheRefresh();
    }
}

public sealed record MessageTemplateCacheRefreshEvent(Guid AggregateId) : EntityCacheRefreshEvent(AggregateId);