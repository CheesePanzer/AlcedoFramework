using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Alcedo.Core.Infrastructure.EfCore;

public static class AlcedoColumnEncryptor
{
    private static string _key = string.Empty;

    internal static void Initialize(string key) => _key = key;

    public static ValueConverter<string?, string?> Converter => new(
        v => v == null ? null : AesGcmEncryption.Encrypt(v, _key),
        v => v == null ? null : AesGcmEncryption.Decrypt(v, _key));
}