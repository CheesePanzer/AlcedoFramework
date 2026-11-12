namespace Alcedo.Frontend.Identity.Clients.Auth;

public sealed record AuthTokenPasswordPhaseResult
{
    public string AccessToken { get; set; } = null!;

    public bool UseTotp { get; set; }

    public Guid? PendingToken { get; set; }

    public bool RequirePasswordReset { get; set; }

    public DateTimeOffset? ValidUntil { get; set; }
}

public sealed record AuthTokenTotpPhaseResult
{
    public string AccessToken { get; set; } = null!;

    public bool RequirePasswordReset { get; set; }

    public DateTimeOffset ValidUntil { get; set; }
}

public sealed record UserProfileDto
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string LoginName { get; set; } = null!;

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? Contact { get; set; }

    public bool RequiresPasswordChange { get; set; }

    public bool UseTwoFactorAuthenticator { get; set; }
}

public sealed record UserChangePasswordCommand
{
    public string OldPassword { get; set; } = null!;

    public string NewPassword { get; set; } = null!;
}