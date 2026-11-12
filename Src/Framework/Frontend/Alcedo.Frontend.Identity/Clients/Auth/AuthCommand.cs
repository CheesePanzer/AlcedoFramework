namespace Alcedo.Frontend.Identity.Clients.Auth;

public sealed record AuthCommand
{
    public required string LoginName { get; set; }

    public required string Password { get; set; }

    public bool IsValid => !string.IsNullOrWhiteSpace(LoginName) && !string.IsNullOrWhiteSpace(Password);
}

public sealed record TotpCommand
{
    public required string PendingToken { get; set; }

    public required string TotpCode { get; set; }
}