namespace Alcedo.Core.Application.SearchQuery;

/// <summary>
/// Represents a single sort field with direction.
/// </summary>
public sealed record SortField
{
    public string Field { get; init; } = string.Empty;
    public bool Descending { get; init; }
}

/// <summary>
/// Encapsulates multi-field sort parameters.
/// Fields are applied in order: first field is primary sort, subsequent fields are tiebreakers.
/// </summary>
public sealed record SortQuery
{
    public IReadOnlyList<SortField>? Fields { get; init; } = null;

    public bool HasSort => Fields?.Count > 0;
}