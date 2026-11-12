namespace Alcedo.Framework.Notification.Application.Dto;

public sealed record MessageTypeDto
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }
    
    public required string JsonExample { get; init; }
}