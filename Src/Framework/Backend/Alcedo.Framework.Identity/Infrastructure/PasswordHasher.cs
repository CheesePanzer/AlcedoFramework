namespace Alcedo.Framework.Identity.Infrastructure;

internal static class PasswordHasher
{
    private const int WorkFactor = 12;

    private static readonly string FakeHash = BCrypt.Net.BCrypt.HashPassword("fakemypassword");

    public static string Hash(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        return BCrypt.Net.BCrypt.HashPassword(plaintext, WorkFactor);
    }

    public static bool Verify(string plaintext, string storedHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        ArgumentException.ThrowIfNullOrWhiteSpace(storedHash);
        return BCrypt.Net.BCrypt.Verify(plaintext, storedHash);
    }

    public static void FakeVerify(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        BCrypt.Net.BCrypt.Verify(plaintext, FakeHash);
    }
}