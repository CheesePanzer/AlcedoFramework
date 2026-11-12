using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Alcedo.Core.Abstractions;
using Alcedo.Framework.Identity.Infrastructure;
using Alcedo.Framework.Shared;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Alcedo.Framework.Identity.Application.Provider;

[RegisterSelf]
internal sealed class JwtProvider
{
    private readonly JwtOptions _jwtOptions;

    private readonly SecurityOptions _securityOptions;

    public JwtProvider(IOptions<JwtOptions> jwtOptions, IOptions<SecurityOptions> securityOptions)
    {
        _jwtOptions = jwtOptions.Value;
        _securityOptions = securityOptions.Value;
    }

    public string GenerateJwt(Guid currentSession, Guid userId, string userCode, string userName)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_jwtOptions.Key);
        var expires = DateTime.UtcNow.AddMinutes(_securityOptions.AccessTokenValidMinutes);
        var claims = new Claim[]
        {
            new Claim(AlcedoClaimNames.CurrentSession, currentSession.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(AlcedoClaimNames.UserCode, userCode),
            new Claim(ClaimTypes.Name, userName),
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                _jwtOptions.Algorithm)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}