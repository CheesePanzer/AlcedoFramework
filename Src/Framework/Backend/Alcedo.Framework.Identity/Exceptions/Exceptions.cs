using Alcedo.Core.Abstractions;

namespace Alcedo.Framework.Identity.Exceptions;

public class IdentityErrInvalidUser : AlcedoUnauthorizedException
{
    public IdentityErrInvalidUser() : base("User does not exist / has been disabled / locked.")
    {
    }

    public IdentityErrInvalidUser(string realReason) : base(realReason,
        "User does not exist / has been disabled / locked.")
    {
    }
}

public class IdentityErrUserNotFound : AlcedoUnauthorizedException
{
    public IdentityErrUserNotFound(Guid id) : base($"User {id} does not exist", "User does not exist")
    {
    }

    public IdentityErrUserNotFound(IReadOnlyCollection<Guid> ids) : base(
        $"Users with {string.Join(";", ids)} do not exist", "Users do not exist")
    {
    }
}

public class IdentityErrUserLocked : AlcedoUnauthorizedException
{
    public IdentityErrUserLocked() : base("User account is locked. Please contact an administrator.")
    {
    }

    public IdentityErrUserLocked(string reason) : base(
        $"User account is locked due to: {reason}. Please contact an administrator.")
    {
    }
}

public class IdentityErrLoginLocked : AlcedoUnauthorizedException
{
    public IdentityErrLoginLocked(int lockMins) : base(
        lockMins >= 0
            ? $"Too many failed attempts. Account locked for {lockMins} minute(s)."
            : "Too many failed attempts. Account is permanently locked. Please contact an administrator.")
    {
    }
}

public class IdentityErrWrongPasswordOrUsername : AlcedoUnauthorizedException
{
    public IdentityErrWrongPasswordOrUsername() : base("Invalid username or password.")
    {
    }

    public IdentityErrWrongPasswordOrUsername(int attemptsLeft) : base(
        $"Invalid username or password. {attemptsLeft} attempt(s) remaining.")
    {
    }
}

public class IdentityErrMfa : AlcedoUnauthorizedException
{
    public IdentityErrMfa() : base("Invalid Mfa code.")
    {
    }
}

public class IdentityErrPasswordIncorrect : AlcedoBadRequestException
{
    public IdentityErrPasswordIncorrect() : base("Incorrect password.")
    {
    }

    public IdentityErrPasswordIncorrect(int attemptsLeft) : base(
        $"Incorrect password. {attemptsLeft} attempt(s) remaining.")
    {
    }
}

public class IdentityErrPasswordTooWeak : AlcedoUnauthorizedException
{
    public IdentityErrPasswordTooWeak() : base("Password's too weak")
    {
    }
}

public class IdentityErrRefreshTokenExpired : AlcedoUnauthorizedException
{
    public IdentityErrRefreshTokenExpired() : base("Invalid Credentials. Please sign in again.")
    {
    }
}

public class IdentityErrLoginStatusExpired : AlcedoUnauthorizedException
{
    public IdentityErrLoginStatusExpired(Guid userId, string reason) : base(
        $"Refresh token {reason} for user {userId}.", "Invalid Credentials. Please sign in again.")
    {
    }
}

public class IdentityErrSessionInvalid : AlcedoUnauthorizedException
{
    private const string Friendly = "Session is no longer valid. Please sign in again.";

    public IdentityErrSessionInvalid(Guid id) : base($"Session is no longer valid For User {id}.", Friendly)
    {
    }

    public IdentityErrSessionInvalid(Guid id, Guid? currentSession) : base(
        $"Session {currentSession.ToString() ?? "Empty"} " +
        $"is no longer valid For User {id}.", Friendly)
    {
    }
}

public class IdentityErrUnauthenticated : AlcedoUnauthorizedException
{
    public IdentityErrUnauthenticated() : base("Invalid Credentials. Please sign in again.")
    {
    }
}

public class IdentityErrNotPermitted : AlcedoForbiddenException
{
    private const string Friendly = "You do not have permission to perform this action.";

    public IdentityErrNotPermitted(Guid userId, string resource) : base(
        $"User {userId} do not have permission to perform endpoint key {resource}.", Friendly)
    {
    }
}

public class IdentityErrNotPermittedPartially : AlcedoForbiddenException
{
    public IdentityErrNotPermittedPartially() : base("You do not have permission to perform this action.")
    {
    }

    public IdentityErrNotPermittedPartially(string reason) : base($"You do not have permission to access: {reason}.")
    {
    }
}

/// <summary>
/// Thrown when the HTTP request carries an invalid or missing authentication credential.
/// Maps to 401.
/// </summary>
public class IdentityErrJwtExpired : AlcedoUnauthorizedException
{
    public IdentityErrJwtExpired() : base("Invalid credentials. Please sign in again.")
    {
        ClientAction = "RefreshAccessToken";
    }
}