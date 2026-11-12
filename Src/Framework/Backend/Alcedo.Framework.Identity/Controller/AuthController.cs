using Alcedo.Core.Presentation;
using Alcedo.Framework.Identity.Application.Command;
using Alcedo.Framework.Identity.Application.Dto.Auth;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Application.Service.Auth;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Controller.Wrapper;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Identity.Controller;

[Area("identity")]
[ApiVersion("1.0")]
[Resource("Auth", "Authorization")]
public sealed class AuthController : AlcedoApiAreaControllerBase
{
    private readonly IAuthApplicationService _authApplicationService;

    private readonly IRefreshTokenCookieProvider _cookieProvider;

    private string CurrentIpAddress => HttpContextIpHelper.GetClientIp(HttpContext);

    public AuthController(IAuthApplicationService authApplicationService, IRefreshTokenCookieProvider cookieProvider)
    {
        _authApplicationService = authApplicationService;
        _cookieProvider = cookieProvider;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [Operation("Login")]
    public async Task<ActionResult<AuthTokenPasswordPhaseResult>> Login(AuthCommand login,
        CancellationToken ct = default)
    {
        var multiToken =
            await _authApplicationService.LoginAsync(login.LoginName, login.Password, CurrentIpAddress, ct);

        if (!multiToken.result.UseTotp)
        {
            _cookieProvider.Set(Response, multiToken.refreshToken);
        }

        return Ok(multiToken.result);
    }

    [HttpPost("twofactor")]
    [AllowAnonymous]
    [Operation("TotpLogin", "Enter Totp Code")]
    public async Task<ActionResult<AuthTokenTotpPhaseResult>> TotpLogin(TotpCommand login,
        CancellationToken ct = default)
    {
        var multiToken =
            await _authApplicationService.VerifyTotpAsync(login.PendingToken, login.TotpCode, CurrentIpAddress, ct);

        _cookieProvider.Set(Response, multiToken.refreshToken);

        return Ok(multiToken.result);
    }

    [HttpPut("logout")]
    [AllowAnonymous]
    [Operation("LogOut")]
    public async Task<IActionResult> LogOut()
    {
        var refreshToken = _cookieProvider.TryGet(Request);

        _cookieProvider.Delete(Response);

        if (refreshToken is null)
        {
            return Ok();
        }

        await _authApplicationService.LogoutAsync(refreshToken.Value);

        return Ok();
    }

    [HttpPost("jwt:refresh")]
    [AllowAnonymous]
    [Operation("RefreshToken")]
    public async Task<ActionResult<string>> RefreshJwt(CancellationToken ct = default)
    {
        var refreshToken = _cookieProvider.Get(Request);

        var result = await _authApplicationService.RefreshJwtAsync(refreshToken, ct);

        _cookieProvider.Set(Response, result.refreshToken);

        return Ok(result.accessToken);
    }

    [HttpPost("cache:refresh")]
    [SkipPermissionCheck]
    [Operation("RefreshCache")]
    public async Task<ActionResult> RefreshUserCache(CancellationToken ct = default)
    {
        await _authApplicationService.RefreshUserCacheAsync(CurrentUser.Id, ct);

        return Ok();
    }

    [HttpGet("profile")]
    [SkipPermissionCheck]
    [Operation("ViewProfile")]
    public async Task<ActionResult<UserProfileDto>> Profile(CancellationToken ct = default)
    {
        var profile = await _authApplicationService.GetUserProfileAsync(CurrentUser.Id, ct);

        return Ok(profile);
    }

    [HttpPost("password:change")]
    [SkipPermissionCheck]
    [Operation("ChangePassword")]
    public async Task<IActionResult> ChangePassword(UserChangePasswordCommand command,
        CancellationToken ct = default)
    {
        await _authApplicationService.ChangePasswordAsync(command, CurrentUser.Id, ct);
        return Ok();
    }

    [HttpPatch("twofactor:enable")]
    [SkipPermissionCheck]
    [Operation("EnableTotp", "Enable Totp Login")]
    public async Task<ActionResult<string>> EnableTwoFactor(CancellationToken ct = default)
    {
        string qrCode = await _authApplicationService.EnableTwoFactorAuthenticationAsync(CurrentUser.Id, ct);
        return Ok(qrCode);
    }

    [HttpPatch("twofactor:disable")]
    [SkipPermissionCheck]
    [Operation("DisableTotp", "Disable Totp Login")]
    public async Task<IActionResult> DisableTwoFactor(CancellationToken ct = default)
    {
        await _authApplicationService.DisableTwoFactorAuthenticationAsync(CurrentUser.Id, ct);
        return Ok();
    }
}
