using Alcedo.Core.Application.SearchQuery;
using Alcedo.Framework.Shared.Application.SearchQuery;

namespace Alcedo.JuiceStore.Backend.Application.SearchQuery;

public sealed record JuiceSearchQuery : MasterDataSearchQueryBase
{
    [StringSearchMode(StringSearchMode.Contains)]
    public string? Flavor { get; init; }

    public bool? IsFeatured { get; init; }
}

public sealed record JuiceCatalogQuery
{
    public string? Keyword { get; init; }

    public string? Flavor { get; init; }

    public bool? Featured { get; init; }
}
