using Alcedo.Core.Abstractions;

namespace Alcedo.Core.Infrastructure.Test;

public record TestConverterAtom(string Value) : ValueAtom(Value);

public class ValueAtomConverterTests
{
    [Fact]
    public void ConvertToProvider_ReturnsStringValue()
    {
        var converter = new ValueAtomConverter<TestConverterAtom>();
        var atom = new TestConverterAtom("hello");

        var result = converter.ConvertToProvider(atom);

        Assert.Equal("hello", result);
    }

    [Fact]
    public void ConvertFromProvider_ReturnsAtomInstance()
    {
        var converter = new ValueAtomConverter<TestConverterAtom>();

        var result = converter.ConvertFromProvider("world");

        var atom = Assert.IsType<TestConverterAtom>(result);
        Assert.Equal("world", atom.Value);
    }

    [Fact]
    public void ConvertFromProvider_Null_ReturnsNull()
    {
        var converter = new ValueAtomConverter<TestConverterAtom>();

        var result = converter.ConvertFromProvider(null);

        Assert.Null(result);
    }
}