using Alcedo.Core.Domain.Entity;

namespace Alcedo.Core.Domain.Test;

public class TestEntity : EntityBase;

public class EntityBaseTests
{
    [Fact]
    public void Id_IsNotEmpty()
    {
        var entity = new TestEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void Id_IsUniquePerInstance()
    {
        var a = new TestEntity();
        var b = new TestEntity();

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Id_IsUuidV7()
    {
        var entity = new TestEntity();
        var bytes = entity.Id.ToByteArray();

        // In .NET Guid.ToByteArray(), the first 3 fields are little-endian.
        // The version field (standard byte 6) lands at index 7.
        Assert.Equal(0x70, bytes[7] & 0xF0);

        // Variant: bits 64-65 = 10 (standard byte 8, index 8)
        Assert.True((bytes[8] & 0xC0) == 0x80);
    }
}