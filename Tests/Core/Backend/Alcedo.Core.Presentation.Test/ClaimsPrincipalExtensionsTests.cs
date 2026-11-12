using System.Security.Claims;

namespace Alcedo.Core.Presentation.Test;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void ToContextUser_ValidNameIdentifier_ParsesGuid()
    {
        var id = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Name, "TestUser")
        }, "TestAuth"));

        var user = principal.ToContextUser();

        Assert.Equal(id, user.Id);
        Assert.Equal("TestUser", user.Name);
        Assert.True(user.IsAuthenticated);
    }

    [Fact]
    public void ToContextUser_InvalidNameIdentifier_ReturnsEmptyGuid()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid")
        }));

        var user = principal.ToContextUser();

        Assert.Equal(Guid.Empty, user.Id);
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public void ToContextUser_MissingNameIdentifier_ReturnsEmptyGuid()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "Anon")
        }));

        var user = principal.ToContextUser();

        Assert.Equal(Guid.Empty, user.Id);
        Assert.Equal("Anon", user.Name);
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public void ToContextUser_Anonymous_ReturnsDefaults()
    {
        var principal = new ClaimsPrincipal();

        var user = principal.ToContextUser();

        Assert.Equal(Guid.Empty, user.Id);
        Assert.Equal("Anonymous", user.Name);
        Assert.False(user.IsAuthenticated);
    }
}
