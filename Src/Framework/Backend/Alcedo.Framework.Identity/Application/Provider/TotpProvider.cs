using System.Web;
using Alcedo.Core.Abstractions;
using Alcedo.Framework.Identity.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OtpNet;

namespace Alcedo.Framework.Identity.Application.Provider;

[RegisterSelf]
public sealed class TotpProvider
{
    private readonly TotpOptions _options;
    private const int SecretByteLength = 20;
    private readonly ILogger<TotpProvider> _logger;

    public TotpProvider(IOptions<TotpOptions> options, ILogger<TotpProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public (string Base32Secret, string QrCodeUri) GenerateSetupInfo(string accountTitle)
    {
        byte[] secretBytes = KeyGeneration.GenerateRandomKey(SecretByteLength);
        string base32Secret = Base32Encoding.ToString(secretBytes);

        string encodedIssuer = HttpUtility.UrlEncode(_options.Issuer);
        string encodedAccount = HttpUtility.UrlEncode(accountTitle);

        string qrCodeUri =
            $"otpauth://totp/{encodedIssuer}:{encodedAccount}?secret={base32Secret}&issuer={encodedIssuer}";

        return (base32Secret, qrCodeUri);
    }

    public string GetQrCodeUri(string accountTitle, string base32Secret)
    {
        string encodedIssuer = HttpUtility.UrlEncode(_options.Issuer);
        string encodedAccount = HttpUtility.UrlEncode(accountTitle);

        return $"otpauth://totp/{encodedIssuer}:{encodedAccount}?secret={base32Secret}&issuer={encodedIssuer}";
    }

    public bool VerifyCode(string base32Secret, string code)
    {
        if (string.IsNullOrWhiteSpace(base32Secret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        try
        {
            byte[] secretBytes = Base32Encoding.ToBytes(base32Secret);

            var totp = new Totp(secretBytes);

            return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        }
        catch
        {
            _logger.LogError("TOTP verification failed because the stored secret is invalid");
            return false;
        }
    }
}