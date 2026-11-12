using Alcedo.Core.Domain.Attributes;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.Event;

namespace Alcedo.Core.Domain.Test;

public sealed record TestEvent(Guid AggregateId) : DomainEventBase(AggregateId);

[AggregateRootDescription("TAR", "TestAggregate")]
public class TestAggregateRoot : AggregateRoot
{
    public void TriggerEvent() => AddEvent(new TestEvent(Id));
}

public class AggregateRootTests
{
    [Fact]
    public void TouchForCreate_SetsVersionToOne()
    {
        var agg = new TestAggregateRoot();

        agg.TouchForCreate();

        Assert.Equal(1, agg.Version);
    }

    [Fact]
    public void TouchForCreate_SetsCreatedAtAndUpdatedAt()
    {
        var agg = new TestAggregateRoot();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        agg.TouchForCreate();

        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        Assert.True(agg.CreatedAt >= before);
        Assert.True(agg.CreatedAt <= after);
        Assert.Equal(agg.CreatedAt, agg.UpdatedAt);
    }

    [Fact]
    public void TouchForCreate_DoesNotOverwriteExistingCreatedAt()
    {
        var agg = new TestAggregateRoot();
        var original = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        typeof(AggregateRoot).GetProperty(nameof(AggregateRoot.CreatedAt))!
            .SetValue(agg, original);

        agg.TouchForCreate();

        Assert.Equal(original, agg.CreatedAt);
        Assert.NotEqual(default, agg.UpdatedAt);
    }

    [Fact]
    public void TouchForUpdate_IncrementsVersion()
    {
        var agg = new TestAggregateRoot();
        agg.TouchForCreate();
        var originalVersion = agg.Version;

        agg.TouchForUpdate();

        Assert.Equal(originalVersion + 1, agg.Version);
    }

    [Fact]
    public void TouchForUpdate_UpdatesUpdatedAtButNotCreatedAt()
    {
        var agg = new TestAggregateRoot();
        agg.TouchForCreate();
        var createdAt = agg.CreatedAt;
        var beforeUpdate = DateTimeOffset.UtcNow.AddSeconds(-1);

        agg.TouchForUpdate();

        var afterUpdate = DateTimeOffset.UtcNow.AddSeconds(1);
        Assert.Equal(createdAt, agg.CreatedAt);
        Assert.True(agg.UpdatedAt >= beforeUpdate);
        Assert.True(agg.UpdatedAt <= afterUpdate);
        Assert.True(agg.UpdatedAt > agg.CreatedAt || agg.UpdatedAt == agg.CreatedAt);
    }

    [Fact]
    public void CollectEventsAndClear_ReturnsEventsAndClearsInternalList()
    {
        var agg = new TestAggregateRoot();
        agg.TouchForCreate();
        agg.TriggerEvent();
        agg.TriggerEvent();

        var events = agg.CollectEventsAndClear();

        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.IsType<TestEvent>(e));

        var secondCollect = agg.CollectEventsAndClear();
        Assert.Empty(secondCollect);
    }

    [Fact]
    public void ClearEvents_RemovesAllEvents()
    {
        var agg = new TestAggregateRoot();
        agg.TriggerEvent();

        agg.ClearEvents();

        Assert.Empty(agg.CollectEventsAndClear());
    }

    [Fact]
    public void MarkAsModified_SetsForceModified()
    {
        var agg = new TestAggregateRoot();

        typeof(AggregateRoot).GetMethod("MarkAsModified",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(agg, null);

        Assert.True(agg.ForceModified);
    }

    [Fact]
    public void ResetModified_ClearsForceModified()
    {
        var agg = new TestAggregateRoot();
        typeof(AggregateRoot).GetMethod("MarkAsModified",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(agg, null);

        agg.ResetModified();

        Assert.False(agg.ForceModified);
    }
}