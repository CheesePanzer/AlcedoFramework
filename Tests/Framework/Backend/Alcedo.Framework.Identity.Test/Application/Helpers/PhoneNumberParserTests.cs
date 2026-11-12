using Alcedo.Framework.Identity.Application.Helper;

namespace Alcedo.Framework.Identity.Test.Application.Helpers;

public class PhoneNumberParserTests
{
    [Fact]
    public void IsValid_CnMobile_ReturnsTrue()
    {
        var result = PhoneNumberFormatValidator.IsValid("+8613800138000", out var error);

        Assert.True(result);
        Assert.Null(error);
    }

    [Fact]
    public void IsValid_MissingPlus_ReturnsFalse()
    {
        var result = PhoneNumberFormatValidator.IsValid("8613800138000", out var error);

        Assert.False(result);
        Assert.Contains("start with '+'", error);
    }

    [Fact]
    public void IsValid_Empty_ReturnsFalse()
    {
        var result = PhoneNumberFormatValidator.IsValid("", out var error);

        Assert.False(result);
        Assert.NotNull(error);
    }

    [Fact]
    public void IsValid_Whitespace_ReturnsFalse()
    {
        var result = PhoneNumberFormatValidator.IsValid("   ", out var error);

        Assert.False(result);
        Assert.Equal("Contact Number is empty", error);
    }

    [Fact]
    public void IsValid_RandomDigits_ReturnsFalse()
    {
        var result = PhoneNumberFormatValidator.IsValid("+12345", out var error);

        Assert.False(result);
    }

    [Fact]
    public void IsValid_TooLong_ReturnsFalse()
    {
        var result = PhoneNumberFormatValidator.IsValid("+" + new string('1', 30), out var error);

        Assert.False(result);
        Assert.Equal("Contact Number is too long", error);
    }
}
