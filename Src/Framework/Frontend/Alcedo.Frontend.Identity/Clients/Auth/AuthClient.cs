using Alcedo.Frontend.Abstraction;

namespace Alcedo.Frontend.Identity.Clients.Auth;

[ApiClient(Area = "identity", Controller = "auth", Version = 1)]
public interface IAuthClient
{
    [HttpPost("login")]
    Task<AuthTokenPasswordPhaseResult> LoginAsync([ToBody] AuthCommand command, CancellationToken ct = default);

    [HttpPost("twofactor")]
    Task<AuthTokenTotpPhaseResult> TotpLoginAsync([ToBody] TotpCommand command, CancellationToken ct = default);

    [HttpPut("logout")]
    Task LogOutAsync(CancellationToken ct = default);

    [HttpPost("jwt:refresh")]
    Task<string> RefreshJwtAsync(CancellationToken ct = default);

    [HttpGet("profile")]
    Task<UserProfileDto> GetProfileAsync(CancellationToken ct = default);

    [HttpPost("password:change")]
    Task ChangePasswordAsync([ToBody] UserChangePasswordCommand command, CancellationToken ct = default);

    [HttpPatch("twofactor:enable")]
    Task<string> EnableTwoFactorAsync(CancellationToken ct = default);

    [HttpPatch("twofactor:disable")]
    Task DisableTwoFactorAsync(CancellationToken ct = default);
}
