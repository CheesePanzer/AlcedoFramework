using Alcedo.Frontend.Abstraction.Attributes;
using Alcedo.Frontend.Abstraction.StateMachine;

namespace Alcedo.Frontend.Abstraction.SearchQuery;

public record MasterDataSearchQueryBase : ReferenceDataSearchQueryBase
{
    public string? Name { get; set; }

    [AutoComponentValueAtomOptions(typeof(MasterDataStates))]
    public IReadOnlyList<MasterDataStateValue>? Status { get; set; }
}