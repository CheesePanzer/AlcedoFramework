namespace Alcedo.Core.Application.Dto;

public abstract record AggregateRootDtoBase : EntityDtoBase
{
    public required int Version { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}