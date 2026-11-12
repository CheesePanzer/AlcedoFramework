namespace Alcedo.Core.Domain.Event;

public abstract record EntityCacheRefreshEvent(Guid AggregateId, int? Version = null)
    : DomainEventBase(AggregateId, Version);