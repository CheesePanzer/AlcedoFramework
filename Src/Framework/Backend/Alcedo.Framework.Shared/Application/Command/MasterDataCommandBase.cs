namespace Alcedo.Framework.Shared.Application.Command;

public abstract record MasterDataCreateCommandBase
{
    public required string Code { get; init; }

    public required string Name { get; init; }
}

public abstract record MasterDataUpdateCommandBase
{
    public required int Version { get; init; }
    public required string Name { get; init; }
}