using Alcedo.Core.Abstractions;
using Alcedo.Framework.Identity.Exceptions;
using Alcedo.Framework.Identity.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Alcedo.Framework.Identity.Controller.Wrapper;

public interface IRefreshTokenCookieProvider
{
    void Set(HttpResponse response, Guid refreshToken);
    Guid Get(HttpRequest request);
    Guid? TryGet(HttpRequest request);
    void Delete(HttpResponse response);
}

[RegisterAs(typeof(IRefreshTokenCookieProvider))]
internal class RefreshTokenCookieProvider : IRefreshTokenCookieProvider
{
    private const string CookieName = "X-Refresh-Token";
    private readonly SecurityOptions _securityOptions;

    public RefreshTokenCookieProvider(IOptions<SecurityOptions> securityOptions)
    {
        _securityOptions = securityOptions.Value;
    }

    public void Set(HttpResponse response, Guid refreshToken)
    {
        if (refreshToken == Guid.Empty)
        {
            throw new ArgumentException("The refresh token cannot be empty");
        }

        var expires = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(_securityOptions.RefreshTokenValidMinutes));
        response.Cookies.Append(CookieName, refreshToken.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure = _securityOptions.CookieSecureOnly,
            SameSite = SameSiteMode.Lax,
            Expires = expires
        });
    }

    public Guid Get(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue(CookieName, out var value) ||
            !Guid.TryParse(value, out var token))
        {
            throw new IdentityErrRefreshTokenExpired();
        }

        return token;
    }

    public Guid? TryGet(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue(CookieName, out var value) ||
            !Guid.TryParse(value, out var token))
        {
            return null;
        }

        return token;
    }

    public void Delete(HttpResponse response)
    {
        response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = _securityOptions.CookieSecureOnly,
            SameSite = SameSiteMode.Lax
        });
    }
}