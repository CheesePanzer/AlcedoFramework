namespace Alcedo.Framework.Identity.Application.Command;

public sealed record AuthCommand
{
    public required string LoginName { get; init; }

    public required string Password { get; init; }
}

public sealed record TotpCommand
{
    public required string PendingToken { get; init; }

    public required string TotpCode { get; init; }
}