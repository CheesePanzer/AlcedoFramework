using System.Text.Json.Serialization;
using Alcedo.Frontend.Abstraction.Dto;

namespace Alcedo.Frontend.Abstraction.SearchQuery;

/// <summary>
/// Represents a single sort field with direction.
/// </summary>
public sealed record SortField
{
    public string Field { get; set; } = string.Empty;
    public bool Descending { get; set; }
}

/// <summary>
/// Encapsulates multi-field sort parameters.
/// Fields are applied in order: first field is primary sort, subsequent fields are tiebreakers.
/// </summary>
public sealed record SortQuery
{
    public IReadOnlyList<SortField>? Fields { get; set; } = null;
    
    [JsonIgnore]
    public bool HasSort => Fields?.Count > 0;
}

public class AggregateRootSortPreset
{
    protected virtual IEnumerable<string> ExtraFields => [];

    public IReadOnlyList<string> Build() =>
    [
        nameof(AggregateRootDtoBase.CreatedAt),
        nameof(AggregateRootDtoBase.UpdatedAt),
        ..ExtraFields
    ];
}

public class ReferenceDataSortPreset : AggregateRootSortPreset
{
    protected override IEnumerable<string> ExtraFields =>
    [
        nameof(ReferenceDataDtoBase.Code),
        ..base.ExtraFields
    ];
}

public class MasterDataSortPreset : ReferenceDataSortPreset
{
    protected override IEnumerable<string> ExtraFields =>
    [
        nameof(MasterDataDtoBase.Name),
        ..base.ExtraFields
    ];
}