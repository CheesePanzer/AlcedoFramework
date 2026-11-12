namespace Alcedo.Framework.Identity.Application.Dto;

public sealed record PermDto
{
    public required string? Area { get; init; }

    public required string Resource { get; init; }

    public required string Operation { get; init; }

    public required string EndpointKey { get; init; }

    public required string ResourceDescription { get; init; }

    public required string OperationDescription { get; init; }
}