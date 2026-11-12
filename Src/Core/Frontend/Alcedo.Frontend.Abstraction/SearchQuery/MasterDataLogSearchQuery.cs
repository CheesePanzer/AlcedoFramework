namespace Alcedo.Frontend.Abstraction.SearchQuery;

public sealed record MasterDataLogSearchQuery : ReferenceDataSearchQueryBase
{
    public Guid AggregateId { get; set; }

    public string? OperatorSnap { get; set; }
}