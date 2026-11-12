using Alcedo.Framework.Identity.Infrastructure;

namespace Alcedo.Framework.Identity.Test.Infrastructure;

public class PasswordHasherTests
{
    [Fact]
    public void HashThenVerify_SamePassword_ReturnsTrue()
    {
        var hash = PasswordHasher.Hash("MyPassword123");
        Assert.True(PasswordHasher.Verify("MyPassword123", hash));
    }

    [Fact]
    public void HashThenVerify_WrongPassword_ReturnsFalse()
    {
        var hash = PasswordHasher.Hash("MyPassword123");
        Assert.False(PasswordHasher.Verify("WrongPassword", hash));
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        var hash1 = PasswordHasher.Hash("SamePassword");
        var hash2 = PasswordHasher.Hash("SamePassword");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hash_EmptyString_Throws()
    {
        Assert.Throws<ArgumentException>(() => PasswordHasher.Hash(""));
    }

    [Fact]
    public void Verify_EmptyPlaintext_Throws()
    {
        Assert.Throws<ArgumentException>(() => PasswordHasher.Verify("", "hashvalue"));
    }

    [Fact]
    public void Verify_EmptyHash_Throws()
    {
        Assert.Throws<ArgumentException>(() => PasswordHasher.Verify("password", ""));
    }

    [Fact]
    public void FakeVerify_DoesNotThrow()
    {
        var ex = Record.Exception(() => PasswordHasher.FakeVerify("anything"));
        Assert.Null(ex);
    }

    [Fact]
    public void FakeVerify_EmptyString_Throws()
    {
        Assert.Throws<ArgumentException>(() => PasswordHasher.FakeVerify(""));
    }
}
