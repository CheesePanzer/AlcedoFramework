namespace Alcedo.Core.Application.SearchQuery;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SearchQueryBuilderIgnoreAttribute : Attribute;

/// <summary>
/// Mark a datetime filed as a range start/ end point;
/// Eg: UpdatedAtStart
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DateRangeFilterAttribute : Attribute
{
    public string EntityPropertyName { get; }
    public DateRangeBound Bound { get; }

    public DateRangeFilterAttribute(string entityPropertyName, DateRangeBound bound)
    {
        EntityPropertyName = entityPropertyName;
        Bound = bound;
    }
}

public enum DateRangeBound
{
    Start,
    End
}

/// <summary>
/// Mark the relevant column of a field in search query
/// eg. [FilterField("Id")] public IReadOnlyList<Guid>? Ids { get; init; }
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FilterFieldAttribute : Attribute
{
    public string EntityPropertyName { get; }

    public FilterFieldAttribute(string entityPropertyName)
    {
        EntityPropertyName = entityPropertyName;
    }
}

public enum StringSearchMode
{
    Exact,
    StartsWith,
    EndsWith,
    Contains
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class StringSearchModeAttribute : Attribute
{
    public StringSearchMode Mode { get; }

    public StringSearchModeAttribute(StringSearchMode mode = StringSearchMode.StartsWith)
    {
        Mode = mode;
    }
}