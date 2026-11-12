using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.Event;
using Alcedo.Core.Infrastructure.EfCore;
using Microsoft.Extensions.Logging;

namespace Alcedo.Core.Infrastructure;

internal sealed class AggregateEventsCollector
{
    private readonly AlcedoDbContext _dbContext;

    private readonly ILogger<AggregateEventsCollector> _logger;

    public AggregateEventsCollector(AlcedoDbContext dbContext, ILogger<AggregateEventsCollector> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    /// <summary>
    /// Collect All Events Generated in every agg
    /// </summary>
    public IReadOnlyList<DomainEventBase> CollectAggregateEvents()
    {
        var aggregates = _dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .ToList();

        var allEvents = new List<DomainEventBase>();

        foreach (var aggregate in aggregates)
        {
            var events = aggregate.CollectEventsAndClear();

            allEvents.AddRange(events);
        }

        _logger.LogDebug("Collected {EventCount} domain events from {AggCount} aggregates",
            allEvents.Count, aggregates.Count);

        return allEvents;
    }

    public void ClearAllEvents()
    {
        var aggregates = _dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(e => e.Entity);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearEvents();
        }
    }
}