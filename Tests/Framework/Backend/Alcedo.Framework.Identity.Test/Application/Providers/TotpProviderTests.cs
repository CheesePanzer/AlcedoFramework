using Alcedo.Framework.Identity.Application.Provider;
using Alcedo.Framework.Identity.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Alcedo.Framework.Identity.Test.Application.Providers;

public class TotpProviderTests
{
    private static TotpProvider CreateProvider(string issuer = "Alcedo")
    {
        var options = Options.Create(new TotpOptions { Issuer = issuer });
        return new TotpProvider(options, NullLogger<TotpProvider>.Instance);
    }

    [Fact]
    public void GenerateSetupInfo_ReturnsNonEmptySecret()
    {
        var provider = CreateProvider();
        var (secret, uri) = provider.GenerateSetupInfo("test@alcedo.com");
        Assert.False(string.IsNullOrEmpty(secret));
        Assert.True(secret.Length > 0);
    }

    [Fact]
    public void GenerateSetupInfo_ReturnsQrCodeUri()
    {
        var provider = CreateProvider();
        var (secret, uri) = provider.GenerateSetupInfo("test@alcedo.com");
        Assert.StartsWith("otpauth://totp/", uri);
        Assert.Contains(secret, uri);
    }

    [Fact]
    public void GenerateSetupInfo_DifferentCallsProduceDifferentSecrets()
    {
        var provider = CreateProvider();
        var (secret1, _) = provider.GenerateSetupInfo("user1");
        var (secret2, _) = provider.GenerateSetupInfo("user2");
        Assert.NotEqual(secret1, secret2);
    }

    [Fact]
    public void VerifyCode_EmptySecret_ReturnsFalse()
    {
        var provider = CreateProvider();
        Assert.False(provider.VerifyCode("", "123456"));
    }

    [Fact]
    public void VerifyCode_EmptyCode_ReturnsFalse()
    {
        var provider = CreateProvider();
        var (secret, _) = provider.GenerateSetupInfo("test@alcedo.com");
        Assert.False(provider.VerifyCode(secret, ""));
    }

    [Fact]
    public void VerifyCode_WrongCode_ReturnsFalse()
    {
        var provider = CreateProvider();
        var (secret, _) = provider.GenerateSetupInfo("test@alcedo.com");
        Assert.False(provider.VerifyCode(secret, "000000"));
    }

    [Fact]
    public void VerifyCode_NullSecret_ReturnsFalse()
    {
        var provider = CreateProvider();
        Assert.False(provider.VerifyCode(null!, "123456"));
    }

    [Fact]
    public void GetQrCodeUri_ReturnsValidUri()
    {
        var provider = CreateProvider("MyApp");
        var secret = "TESTSECRET123";
        var uri = provider.GetQrCodeUri("user@test.com", secret);
        Assert.StartsWith("otpauth://totp/", uri);
        Assert.Contains(secret, uri);
        Assert.Contains("MyApp", uri);
    }
}
