namespace Alcedo.Core.Application.SearchQuery;

/// <summary>
/// Base class for all paged search queries.
/// Subclass properties are automatically mapped to filter conditions by SearchQueryBuilder.
/// All nullable properties are skipped when null (treated as "no filter").
/// </summary>
public record EntitySearchQueryBase;

public record AggregateRootSearchQueryBase : EntitySearchQueryBase
{
    [DateRangeFilter("CreatedAt", DateRangeBound.Start)]
    public DateTimeOffset? CreatedAtStart { get; init; }

    [DateRangeFilter("CreatedAt", DateRangeBound.End)]
    public DateTimeOffset? CreatedAtEnd { get; init; }

    [DateRangeFilter("UpdatedAt", DateRangeBound.Start)]
    public DateTimeOffset? UpdatedAtStart { get; init; }

    [DateRangeFilter("UpdatedAt", DateRangeBound.End)]
    public DateTimeOffset? UpdatedAtEnd { get; init; }
}