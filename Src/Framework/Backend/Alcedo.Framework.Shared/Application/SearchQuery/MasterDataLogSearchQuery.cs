namespace Alcedo.Framework.Shared.Application.SearchQuery;

public sealed record MasterDataLogSearchQuery : ReferenceDataSearchQueryBase
{
    public Guid AggregateId { get; init; }

    public string? OperatorSnap { get; init; }
}