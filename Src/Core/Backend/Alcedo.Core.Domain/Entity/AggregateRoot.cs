using Alcedo.Core.Domain.Event;

namespace Alcedo.Core.Domain.Entity;

public abstract class AggregateRoot : EntityBase
{
    public int Version { get; protected set; }

    /// <summary>
    /// Not Mapped to Database, as a convenient trigger for version ++
    /// </summary>
    public bool ForceModified { get; private set; }

    public DateTimeOffset CreatedAt { get; protected set; }

    public DateTimeOffset UpdatedAt { get; protected set; }

    private readonly List<DomainEventBase> _events = new();

    protected void AddEvent(DomainEventBase @event)
    {
        _events.Add(@event);
    }

    /// <summary>
    /// Collect events and clear origin list;
    /// </summary>
    /// <returns></returns>
    internal virtual ICollection<DomainEventBase> CollectEventsAndClear()
    {
        // copy to a new list
        var events = _events.ToList();
        // clear origin
        _events.Clear();
        return events;
    }

    internal void ClearEvents()
    {
        _events.Clear();
    }

    protected AggregateRoot()
    {
    }

    internal void TouchForCreate()
    {
        Version = 1;
        var now = DateTimeOffset.UtcNow;

        if (CreatedAt == default)
        {
            CreatedAt = now;
        }

        if (UpdatedAt == default)
        {
            UpdatedAt = now;
        }
    }

    internal void TouchForUpdate()
    {
        Version++;

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    internal void ResetModified()
    {
        ForceModified = false;
    }

    protected void MarkAsModified()
    {
        ForceModified = true;
    }
}