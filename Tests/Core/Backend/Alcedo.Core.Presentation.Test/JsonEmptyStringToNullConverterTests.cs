using System.Text.Json;
using Alcedo.Core.Presentation.RequestJsonConverter;

namespace Alcedo.Core.Presentation.Test;

public class JsonEmptyStringToNullConverterTests
{
    private static JsonSerializerOptions OptionsWithConverter()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonEmptyStringToNullConverter());
        return options;
    }

    [Fact]
    public void Read_EmptyString_ReturnsNull()
    {
        var json = "\"\"";
        var result = JsonSerializer.Deserialize<string?>(json, OptionsWithConverter());

        Assert.Null(result);
    }

    [Fact]
    public void Read_Whitespace_ReturnsWhitespace()
    {
        var json = "\"   \"";
        var result = JsonSerializer.Deserialize<string?>(json, OptionsWithConverter());

        Assert.Equal("   ", result);
    }

    [Fact]
    public void Read_NullToken_ReturnsNull()
    {
        var json = "null";
        var result = JsonSerializer.Deserialize<string?>(json, OptionsWithConverter());

        Assert.Null(result);
    }

    [Fact]
    public void Read_NormalString_ReturnsValue()
    {
        var json = "\"hello\"";
        var result = JsonSerializer.Deserialize<string?>(json, OptionsWithConverter());

        Assert.Equal("hello", result);
    }

    [Fact]
    public void Write_WritesValueDirectly()
    {
        var options = OptionsWithConverter();
        var result = JsonSerializer.Serialize("world", options);

        Assert.Equal("\"world\"", result);
    }
}