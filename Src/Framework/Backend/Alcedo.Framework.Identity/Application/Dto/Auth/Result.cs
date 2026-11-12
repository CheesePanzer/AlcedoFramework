namespace Alcedo.Framework.Identity.Application.Dto.Auth;

public sealed record AuthTokenPasswordPhaseResult
{
    public required string AccessToken { get; init; }

    public required bool UseTotp { get; init; }

    public required Guid? PendingToken { get; init; }

    public required bool RequirePasswordReset { get; init; }

    public required DateTimeOffset? ValidUntil { get; init; }
}

public sealed record AuthTokenTotpPhaseResult
{
    public required string AccessToken { get; init; }

    public required bool RequirePasswordReset { get; init; }

    public required DateTimeOffset ValidUntil { get; init; }
}