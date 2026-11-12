using Alcedo.Framework.Identity.Application.Helper;

namespace Alcedo.Framework.Identity.Test.Application.Helpers;

public class EmailFormatCheckerTests
{
    [Fact]
    public void IsValid_StandardEmail_ReturnsTrue()
    {
        var result = EmailFormatChecker.IsValid("user@example.com", out var error);

        Assert.True(result);
        Assert.Null(error);
    }

    [Fact]
    public void IsValid_SubdomainEmail_ReturnsTrue()
    {
        var result = EmailFormatChecker.IsValid("user@mail.example.com", out var error);

        Assert.True(result);
        Assert.Null(error);
    }

    [Fact]
    public void IsValid_Null_ReturnsFalse()
    {
        var result = EmailFormatChecker.IsValid(null, out var error);

        Assert.False(result);
        Assert.Equal("email is empty", error);
    }

    [Fact]
    public void IsValid_Empty_ReturnsFalse()
    {
        var result = EmailFormatChecker.IsValid("", out var error);

        Assert.False(result);
        Assert.Equal("email is empty", error);
    }

    [Fact]
    public void IsValid_NoAtSymbol_ReturnsFalse()
    {
        var result = EmailFormatChecker.IsValid("notanemail", out var error);

        Assert.False(result);
        Assert.Equal("email is invalid", error);
    }

    [Fact]
    public void IsValid_OverMaxLength_ReturnsFalse()
    {
        // 256 chars: over the 255 limit, fails before format check
        var email = new string('a', 251) + "@example.com";
        var result = EmailFormatChecker.IsValid(email, out var error);
        Assert.False(result);
        Assert.Equal("email is too long", error);
    }
}
