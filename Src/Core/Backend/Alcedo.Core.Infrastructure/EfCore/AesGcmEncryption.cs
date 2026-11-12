using System.Security.Cryptography;
using System.Text;

namespace Alcedo.Core.Infrastructure.EfCore;

public static class AesGcmEncryption
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static string Encrypt(string plaintext, string base64Key)
    {
        var key = Convert.FromBase64String(base64Key);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[NonceSize + TagSize + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, NonceSize);
        ciphertext.CopyTo(result, NonceSize + TagSize);

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string base64Ciphertext, string base64Key)
    {
        var key = Convert.FromBase64String(base64Key);
        var data = Convert.FromBase64String(base64Ciphertext);

        var nonce = data[..NonceSize];
        var tag = data[NonceSize..(NonceSize + TagSize)];
        var ciphertext = data[(NonceSize + TagSize)..];

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}