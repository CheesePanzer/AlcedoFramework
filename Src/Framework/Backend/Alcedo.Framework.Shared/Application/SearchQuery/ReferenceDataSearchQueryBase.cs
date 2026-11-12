using Alcedo.Core.Application.SearchQuery;

namespace Alcedo.Framework.Shared.Application.SearchQuery;

public record ReferenceDataSearchQueryBase : AggregateRootSearchQueryBase
{
    public string? Code { get; init; }
}