using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Alcedo.Framework.Identity.Application.Provider;
using Alcedo.Framework.Identity.Infrastructure;
using Alcedo.Framework.Shared;
using Microsoft.Extensions.Options;

namespace Alcedo.Framework.Identity.Test.Application.Providers;

public class JwtProviderTests
{
    private static readonly string TestKey = Convert.ToBase64String(
        System.Text.Encoding.UTF8.GetBytes("a-32-character-secret-key-ok!"));

    private static JwtProvider CreateProvider(string issuer = "TestIssuer", string audience = "TestAudience",
        string algorithm = "HS256", string? key = null, int accessTokenValidMinutes = 15)
    {
        var jwtOpts = Options.Create(new JwtOptions
            { Issuer = issuer, Audience = audience, Algorithm = algorithm, Key = key ?? TestKey });
        var secOpts = Options.Create(new SecurityOptions { AccessTokenValidMinutes = accessTokenValidMinutes });
        return new JwtProvider(jwtOpts, secOpts);
    }

    [Fact]
    public void GenerateJwt_ReturnsNonEmptyString()
    {
        var provider = CreateProvider();
        var token = provider.GenerateJwt(Guid.NewGuid(), Guid.NewGuid(), "USR01", "Alice");
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public void GenerateJwt_ContainsCorrectClaims()
    {
        var provider = CreateProvider();
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = provider.GenerateJwt(sessionId, userId, "USR01", "Alice");
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.Equal(sessionId.ToString(), jwt.Claims.First(c => c.Type == AlcedoClaimNames.CurrentSession).Value);
        Assert.Equal(userId.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("USR01", jwt.Claims.First(c => c.Type == AlcedoClaimNames.UserCode).Value);
        Assert.Equal("Alice", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
    }

    [Fact]
    public void GenerateJwt_HasIssuerAndAudience()
    {
        var provider = CreateProvider("MyIssuer", "MyAudience");
        var token = provider.GenerateJwt(Guid.NewGuid(), Guid.NewGuid(), "USR01", "Alice");
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.Equal("MyIssuer", jwt.Issuer);
        Assert.Contains("MyAudience", jwt.Audiences);
    }

    [Fact]
    public void GenerateJwt_TokenExpiresWithinConfiguredMinutes()
    {
        var provider = CreateProvider(accessTokenValidMinutes: 15);
        var token = provider.GenerateJwt(Guid.NewGuid(), Guid.NewGuid(), "USR01", "Alice");
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var expectedExpiry = DateTime.UtcNow.AddMinutes(15);
        Assert.True(jwt.ValidTo <= expectedExpiry.AddSeconds(5));
        Assert.True(jwt.ValidTo >= expectedExpiry.AddSeconds(-5));
    }
}
