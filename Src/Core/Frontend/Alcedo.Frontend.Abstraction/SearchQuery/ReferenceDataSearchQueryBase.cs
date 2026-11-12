namespace Alcedo.Frontend.Abstraction.SearchQuery;

public record ReferenceDataSearchQueryBase : AggregateRootSearchQueryBase
{
    public string? Code { get; set; }
}