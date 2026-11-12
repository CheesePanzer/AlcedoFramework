using Alcedo.Framework.Identity.Application.Helper;

namespace Alcedo.Framework.Identity.Test.Application.Helpers;

public class PasswordStrengthCheckerTests
{
    [Fact]
    public void IsStrong_EmptyPassword_ReturnsFalse()
    {
        Assert.False(PasswordStrengthChecker.IsStrong(""));
    }

    [Fact]
    public void IsStrong_NullPassword_ReturnsFalse()
    {
        Assert.False(PasswordStrengthChecker.IsStrong(null!));
    }

    [Fact]
    public void IsStrong_ShortSimple_ReturnsFalse()
    {
        Assert.False(PasswordStrengthChecker.IsStrong("abc"));
    }

    [Fact]
    public void IsStrong_EightCharsButAllLowercase_ReturnsFalse()
    {
        Assert.False(PasswordStrengthChecker.IsStrong("abcdefgh"));
    }

    [Fact]
    public void IsStrong_StrongPassword_ReturnsTrue()
    {
        // Length >= 8, upper, digit, special ¡ª zxcvbn score >= 3
        Assert.True(PasswordStrengthChecker.IsStrong("Tr0ub4dor&3"));
    }

    [Fact]
    public void IsStrong_AnotherStrong_ReturnsTrue()
    {
        Assert.True(PasswordStrengthChecker.IsStrong("c0rrEct-h0rse-bAtt3ry"));
    }
}
