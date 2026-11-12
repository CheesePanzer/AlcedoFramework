using Alcedo.Core.Domain;
using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.Event;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Framework.Shared.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.Notification.Domain.Entity;

public sealed class MessageConfigAggRoot : MasterDataAggregateRootBase
{
    protected override StateActionGuardBase<MasterDataStateValue, MasterDataStateAction> ActionGuard =>
        new MessageConfigActionGuard();

    public string MessageTypeCode { get; private set; }
    
    public bool ShouldSkip { get; private set; }

    private List<MessageConfigTemplateRelationsEntity> _templatesRelations =
        new List<MessageConfigTemplateRelationsEntity>();

    public IReadOnlyList<MessageConfigTemplateRelationsEntity> TemplatesRelations => _templatesRelations;

    public IReadOnlyList<Guid> TemplateIds => _templatesRelations.Select(t => t.MessageTemplateId).ToList();

    public bool IsActive => Status == MasterDataStates.Enabled;

#pragma warning disable CS8618
    private MessageConfigAggRoot()
    {
    }
#pragma warning restore CS8618

    public MessageConfigAggRoot(
        string code,
        string name,
        string messageTypeCode,
        OperatorRecord creator) : base(code, name, creator)
    {
        MessageTypeCode = messageTypeCode;
    }

    public void Update(
        string name,
        string messageTypeCode,
        OperatorRecord updater)
    {
        base.Update(name, updater);

        MessageTypeCode = messageTypeCode;

        AddMessageConfigCacheRefreshEvent();
    }

    private void AddMessageConfigCacheRefreshEvent()
    {
        AddEvent(new MessageConfigCacheRefreshEvent(Id));
    }

    public void BatchAssignTemplates(IReadOnlyCollection<Guid> templateIds, OperatorRecord oper)
    {
        foreach (var templateId in templateIds)
        {
            if (TemplateIds.Contains(templateId))
            {
                continue;
            }

            _templatesRelations.Add(new MessageConfigTemplateRelationsEntity(Id, templateId));
        }

        ExecuteAction(MessageConfigStateActions.BatchAssignTemplates, oper, string.Join(";", templateIds));
        AddMessageConfigCacheRefreshEvent();
    }

    public void BatchRemoveTemplates(IReadOnlyCollection<Guid> templateIds, OperatorRecord oper)
    {
        var toRemove = _templatesRelations.Where(t => templateIds.Contains(t.MessageTemplateId)).ToList();
        foreach (var templateRelationEntity in toRemove)
        {
            _templatesRelations.Remove(templateRelationEntity);
        }

        ExecuteAction(MessageConfigStateActions.BatchAssignTemplates, oper, string.Join(";", templateIds));
        AddMessageConfigCacheRefreshEvent();
    }

    public void ClearTemplates(OperatorRecord oper)
    {
        _templatesRelations.Clear();

        ExecuteAction(MessageConfigStateActions.BatchAssignTemplates, oper);
        AddMessageConfigCacheRefreshEvent();
    }

    public override void Enable(OperatorRecord oper)
    {
        if (_templatesRelations.Count == 0)
        {
            throw new DomErrRuleViolation("At least one template must be provided before enabling config");
        }
        base.Enable(oper);
        AddMessageConfigCacheRefreshEvent();
    }

    public override void Disable(OperatorRecord oper)
    {
        base.Disable(oper);
        AddMessageConfigCacheRefreshEvent();
    }

    public void SetConfigSkipped(OperatorRecord oper)
    {
        ShouldSkip = true;
        ExecuteAction(MessageConfigStateActions.Skip, oper);
        AddMessageConfigCacheRefreshEvent();
    }

    public void SetConfigNotSkipped(OperatorRecord oper)
    {
        ShouldSkip = false;
        ExecuteAction(MessageConfigStateActions.UnSkip, oper);
        AddMessageConfigCacheRefreshEvent();
    }
}

public sealed class MessageConfigTemplateRelationsEntity : EntityBase
{
    public Guid MessageConfigId { get; private set; }

    public Guid MessageTemplateId { get; private set; }

    public MessageConfigTemplateRelationsEntity(Guid messageConfigId, Guid messageTemplateId)
    {
        MessageConfigId = messageConfigId;

        MessageTemplateId = messageTemplateId;
    }
}

public sealed record MessageConfigCacheRefreshEvent(Guid AggregateId) : EntityCacheRefreshEvent(AggregateId);

internal class MessageConfigReferenceConfiguration : IAggregateRootReferenceConfiguration<MessageConfigAggRoot>
{
    public void Configure(ReferenceEntityBuilder<MessageConfigAggRoot> builder)
    {
        builder
            .Through(cfg => cfg.TemplatesRelations)
            .References<MessageTemplateAggRoot>(ur => ur.MessageTemplateId);
    }
}

internal class MessageConfigStateActions : MasterDataStateActions
{
    public static readonly MasterDataStateAction BatchAssignTemplates = new MasterDataStateAction("BatchAssignTemplates");

    public static readonly MasterDataStateAction BatchDeleteTemplates =
        new MasterDataStateAction("BatchDeleteTemplates");

    public static readonly MasterDataStateAction ClearTemplates = new MasterDataStateAction("ClearTemplates");
    
    public static readonly MasterDataStateAction Skip = new MasterDataStateAction("Skip");
    
    public static readonly MasterDataStateAction UnSkip = new MasterDataStateAction("UnSkip");
}

internal class MessageConfigActionGuard : MasterDataDefaultOperationGuard
{
    protected override IReadOnlyDictionary<string, IReadOnlySet<MasterDataStateValue>> Additional =>
        new Dictionary<string, IReadOnlySet<MasterDataStateValue>>
        {
            [MessageConfigStateActions.BatchAssignTemplates.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Draft, MasterDataStates.Enabled, MasterDataStates.Disabled },
            [MessageConfigStateActions.BatchDeleteTemplates.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Draft, MasterDataStates.Enabled, MasterDataStates.Disabled },
            [MessageConfigStateActions.ClearTemplates.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Draft, MasterDataStates.Enabled, MasterDataStates.Disabled },
            [MessageConfigStateActions.Skip.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Draft, MasterDataStates.Enabled, MasterDataStates.Disabled },
            [MessageConfigStateActions.UnSkip.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Draft, MasterDataStates.Enabled, MasterDataStates.Disabled },
        };
}