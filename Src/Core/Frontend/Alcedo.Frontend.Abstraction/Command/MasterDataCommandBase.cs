using Alcedo.Frontend.Abstraction.Attributes;

namespace Alcedo.Frontend.Abstraction.Command;

public abstract record MasterDataCreateCommandBase
{
    public string Code { get; init; } = null!;

    public string Name { get; init; } = null!;
}

public abstract record MasterDataUpdateCommandBase
{
    [AutoFormIgnoreRender] public int Version { get; init; }
    public string Name { get; init; } = null!;
}