using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.Kit.Test;
using Alcedo.Framework.Notification.Contracts;
using Alcedo.Framework.Notification.Domain.Entity;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Notification.Test;

public class NotificationDomainFixture
{
    public NotificationDomainFixture() => MetadataCenterTestExtensions.InitializeForTest(typeof(MessageConfigAggRoot).Assembly);
}

[CollectionDefinition("NotificationDomain")]
public class NotificationDomainCollection : ICollectionFixture<NotificationDomainFixture> { }

[Collection("NotificationDomain")]
public class NotificationDomainTests
{
    private static OperatorRecord Oper() => new(Guid.NewGuid(), "TEST", "Tester");
    private static MessageConfigAggRoot Config() => new("ORDER", "Order notification", "ORDER_PAID", Oper());
    private static MessageTemplateAggRoot Template() => new("EMAIL", "Order email", NotificationChannel.Email,
        "Order paid", "Thank you", null, Oper());

    [Fact]
    public void Enable_RequiresAtLeastOneTemplateAndLeavesDraftOnFailure()
    {
        var config = Config();
        config.ClearEventsForTest();
        Assert.Throws<DomErrRuleViolation>(() => config.Enable(Oper()));
        Assert.Equal(MasterDataStates.Draft, config.Status);
        Assert.Empty(config.CollectEventsAndClearForTest());
    }

    [Fact]
    public void AssignTemplates_DeduplicatesIdsAndLinksRelationsToConfig()
    {
        var config = Config();
        var id = Guid.NewGuid();
        config.ClearEventsForTest();
        config.BatchAssignTemplates([id, id], Oper());
        var relation = Assert.Single(config.TemplatesRelations);
        Assert.Equal(config.Id, relation.MessageConfigId);
        Assert.Equal(id, relation.MessageTemplateId);
        Assert.Contains(config.CollectEventsAndClearForTest(), e => e is MessageConfigCacheRefreshEvent evt && evt.AggregateId == config.Id);
    }

    [Fact]
    public void RemoveTemplates_PreservesUnselectedRelations()
    {
        var config = Config();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        config.BatchAssignTemplates([first, second], Oper());
        config.BatchRemoveTemplates([first, Guid.NewGuid()], Oper());
        Assert.Equal(second, Assert.Single(config.TemplateIds));
    }

    [Fact]
    public void ClearTemplates_RemovesAllRelations()
    {
        var config = Config();
        config.BatchAssignTemplates([Guid.NewGuid(), Guid.NewGuid()], Oper());
        config.ClearTemplates(Oper());
        Assert.Empty(config.TemplateIds);
    }

    [Fact]
    public void EnableDisableEnable_ChangesDeliveryEligibilityAndPublishesInvalidation()
    {
        var config = Config();
        config.BatchAssignTemplates([Guid.NewGuid()], Oper());
        config.ClearEventsForTest();
        config.Enable(Oper());
        Assert.True(config.IsActive);
        config.Disable(Oper());
        Assert.False(config.IsActive);
        config.Enable(Oper());
        Assert.True(config.IsActive);
        Assert.Equal(3, config.CollectEventsAndClearForTest().OfType<MessageConfigCacheRefreshEvent>().Count());
    }

    [Fact]
    public void SkipUnskip_ChangesSkipFlagWithoutDisablingConfig()
    {
        var config = Config();
        config.BatchAssignTemplates([Guid.NewGuid()], Oper());
        config.Enable(Oper());
        config.SetConfigSkipped(Oper());
        Assert.True(config.ShouldSkip);
        Assert.True(config.IsActive);
        config.SetConfigNotSkipped(Oper());
        Assert.False(config.ShouldSkip);
        Assert.True(config.IsActive);
    }

    [Fact]
    public void UpdateConfig_ChangesMessageTypeAndPublishesInvalidation()
    {
        var config = Config();
        config.ClearEventsForTest();
        config.Update("Shipment", "ORDER_SHIPPED", Oper());
        Assert.Equal("ORDER_SHIPPED", config.MessageTypeCode);
        Assert.Equal("Shipment", config.Name);
        Assert.Single(config.CollectEventsAndClearForTest().OfType<MessageConfigCacheRefreshEvent>());
    }

    [Fact]
    public void Template_EnableDisable_ChangesEligibilityAndPublishesInvalidation()
    {
        var template = Template();
        Assert.False(template.IsActive);
        template.ClearEventsForTest();
        template.Enable(Oper());
        Assert.True(template.IsActive);
        template.Disable(Oper());
        Assert.False(template.IsActive);
        Assert.Equal(2, template.CollectEventsAndClearForTest().OfType<MessageTemplateCacheRefreshEvent>().Count());
    }

    [Fact]
    public void Template_UpdateChangesChannelAndContentAndPublishesInvalidation()
    {
        var template = Template();
        template.ClearEventsForTest();
        template.Update("SMS", NotificationChannel.Sms, null, "Shipped", "EXT-1", Oper());
        Assert.Equal(NotificationChannel.Sms, template.Channel);
        Assert.Null(template.TitleTemplate);
        Assert.Equal("Shipped", template.BodyTemplate);
        Assert.Equal("EXT-1", template.ExternalTemplateId);
        Assert.Single(template.CollectEventsAndClearForTest().OfType<MessageTemplateCacheRefreshEvent>());
    }

    [Fact]
    public void Template_DisableDraft_IsRejected()
    {
        var template = Template();
        Assert.Throws<DomErrStatusInvalid>(() => template.Disable(Oper()));
        Assert.Equal(MasterDataStates.Draft, template.Status);
    }
}
