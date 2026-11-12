using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Shared.Application.SearchQuery;

public record MasterDataSearchQueryBase : ReferenceDataSearchQueryBase
{
    public string? Name { get; init; }

    public IReadOnlyList<MasterDataStateValue>? Status { get; init; }
}