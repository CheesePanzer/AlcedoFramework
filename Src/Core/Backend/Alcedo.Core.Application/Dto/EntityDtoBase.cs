namespace Alcedo.Core.Application.Dto;

public abstract record EntityDtoBase
{
    public required Guid Id { get; init; }
}