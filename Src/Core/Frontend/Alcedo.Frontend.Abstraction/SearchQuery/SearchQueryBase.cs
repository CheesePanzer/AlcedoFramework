namespace Alcedo.Frontend.Abstraction.SearchQuery;

/// <summary>
/// Base class for all paged search queries.
/// Subclass properties are automatically mapped to filter conditions by SearchQueryBuilder.
/// All nullable properties are skipped when null (treated as "no filter").
/// </summary>
public record EntitySearchQueryBase;

public record AggregateRootSearchQueryBase : EntitySearchQueryBase
{
    public DateTimeOffset? CreatedAtStart { get; set; }

    public DateTimeOffset? CreatedAtEnd { get; set; }

    public DateTimeOffset? UpdatedAtStart { get; set; }

    public DateTimeOffset? UpdatedAtEnd { get; set; }
}