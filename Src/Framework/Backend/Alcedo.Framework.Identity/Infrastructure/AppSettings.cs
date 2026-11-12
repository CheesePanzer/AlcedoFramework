using Alcedo.Core.Abstractions;

namespace Alcedo.Framework.Identity.Infrastructure;

public sealed class JwtOptions : AppSettingsBase
{
    [AppSettingsDemo("Alcedo", "JWT token issuer identifier.")]
    public string Issuer { get; init; } = string.Empty;

    [AppSettingsDemo("AlcedoUser", "JWT token audience identifier.")]
    public string Audience { get; init; } = string.Empty;

    [AppSettingsDemo("HS256", "JWT signing algorithm.")]
    public string Algorithm { get; init; } = "HS256";

    [AppSettingsDemo("",
        "Base64-encoded HMAC secret key. Generate with: python -c \"import os,base64; print(base64.b64encode(os.urandom(32)).decode())\"")]
    public string Key { get; init; } = string.Empty;
}

public sealed class SecurityOptions : AppSettingsBase
{
    [AppSettingsDemo("60", "TTL in seconds for the login failure counter in Redis. Resets after this period.")]
    public int LoginFailedTTLSeconds { get; init; } = 60;

    [AppSettingsDemo("5", "Number of consecutive login failures before the account is permanently locked.")]
    public int AccountLockThresholdCount { get; init; } = 5;

    [AppSettingsDemo("180",
        "Validity window in seconds for a login context (e.g. TOTP verification after password check).")]
    public int LoginContextValiditySeconds { get; init; } = 180;

    [AppSettingsDemo("15", "Access token validity in minutes.")]
    public int AccessTokenValidMinutes { get; init; } = 15;

    [AppSettingsDemo("480", "Refresh token validity in minutes.")]
    public int RefreshTokenValidMinutes { get; init; } = 480;
    
    [AppSettingsDemo("true", "If true, Refresh token will only be sent under https.")]
    public bool CookieSecureOnly { get; init; } = true;
}

public sealed class TotpOptions : AppSettingsBase
{
    [AppSettingsDemo("Alcedo", "Issuer name displayed in the authenticator app (e.g. Google Authenticator).")]
    public string Issuer { get; init; } = "Alcedo";
}

public sealed class RbacSchemeOptions : AppSettingsBase
{
    [AppSettingsDemo("120", "Default Value For Role Cache Expiry")]
    public int RoleCacheExpiryMinutes { get; init; } = 120;
}