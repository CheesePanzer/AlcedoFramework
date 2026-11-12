using Alcedo.Core.Abstractions;

namespace Alcedo.Core.Domain.Event;

public abstract record DomainEventBase(Guid AggregateId, int? Version = null)
    : EventRoot;