using System.Security.Cryptography;

namespace Alcedo.Core.Domain.Helper;

public static class TokenExtensions
{
    public static string ToSha256Hash(this Guid token)
    {
        Span<byte> bytes = stackalloc byte[16];
        token.TryWriteBytes(bytes);

        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}