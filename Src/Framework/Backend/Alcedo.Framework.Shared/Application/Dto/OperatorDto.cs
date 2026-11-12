namespace Alcedo.Framework.Shared.Application.Dto;

public record OperatorDto
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }
}