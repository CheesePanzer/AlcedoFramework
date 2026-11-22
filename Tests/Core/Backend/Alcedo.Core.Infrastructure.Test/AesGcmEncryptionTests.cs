using System.Security.Cryptography;
using Alcedo.Core.Infrastructure.EfCore;

namespace Alcedo.Core.Infrastructure.Test;

public class AesGcmEncryptionTests
{
    private static string Key() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    [Theory]
    [InlineData("")]
    [InlineData("secret")]
    [InlineData("你好，Alcedo 🔐")]
    public void EncryptDecrypt_RoundTripsUtf8AndEmptyContent(string plaintext)
    {
        var key = Key();
        var encrypted = AesGcmEncryption.Encrypt(plaintext, key);
        Assert.NotEqual(plaintext, encrypted);
        Assert.Equal(plaintext, AesGcmEncryption.Decrypt(encrypted, key));
    }

    [Fact]
    public void Encrypt_SamePlaintextUsesFreshNonce()
    {
        var key = Key();
        var first = AesGcmEncryption.Encrypt("same secret", key);
        var second = AesGcmEncryption.Encrypt("same secret", key);
        Assert.NotEqual(first, second);
        Assert.Equal("same secret", AesGcmEncryption.Decrypt(first, key));
        Assert.Equal("same secret", AesGcmEncryption.Decrypt(second, key));
    }

    [Fact]
    public void Decrypt_RejectsWrongKey()
    {
        var encrypted = AesGcmEncryption.Encrypt("secret", Key());
        Assert.ThrowsAny<CryptographicException>(() => AesGcmEncryption.Decrypt(encrypted, Key()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    [InlineData(28)]
    public void Decrypt_RejectsTamperedNonceTagOrCiphertext(int offset)
    {
        var key = Key();
        var encrypted = Convert.FromBase64String(AesGcmEncryption.Encrypt("secret", key));
        encrypted[offset] ^= 1;
        Assert.ThrowsAny<CryptographicException>(() => AesGcmEncryption.Decrypt(Convert.ToBase64String(encrypted), key));
    }
}
