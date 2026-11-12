namespace Alcedo.Framework.Identity.Application.Dto.User;

public sealed record UserProfileDto
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string LoginName { get; init; }

    public required string? UserName { get; init; }

    public required string? Email { get; init; }

    public required string? Contact { get; init; }

    public required bool RequiresPasswordChange { get; init; }

    public required bool UseTwoFactorAuthenticator { get; init; }
}