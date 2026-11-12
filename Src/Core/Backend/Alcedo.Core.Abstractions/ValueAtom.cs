using System.Text.Json;
using System.Text.Json.Serialization;

namespace Alcedo.Core.Abstractions;

[JsonConverter(typeof(ValueAtomConverterFactory))]
public abstract record ValueAtom(string Value);

public class ValueAtomJsonConverter<T> : JsonConverter<T> where T : ValueAtom
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value is null)
        {
            return null;
        }

        return (T)Activator.CreateInstance(typeof(T), value)!;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public class ValueAtomConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(ValueAtom).IsAssignableFrom(typeToConvert) && !typeToConvert.IsAbstract;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(ValueAtomJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}