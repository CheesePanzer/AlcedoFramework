using Alcedo.Frontend.Abstraction.Attributes;
using Alcedo.Frontend.Abstraction.StateMachine;

namespace Alcedo.Frontend.Abstraction.Dto;

public abstract record MasterDataDtoBase : ReferenceDataDtoBase
{
    public string Name { get; set; } = null!;

    [AutoComponentValueAtomOptions(typeof(MasterDataStates))]
    public MasterDataStateValue Status { get; set; } = null!;

    [AutoFormIgnoreRender]
    [AutoTableIgnoreRender]
    public OperatorDto Creator { get; set; } = null!;

    [AutoFormIgnoreRender]
    [AutoTableIgnoreRender]
    public OperatorDto Updater { get; set; } = null!;

    public string GetSnap() => $"[{Code}] {Name}";
}