using System.Data;
using System.Text.Json;
using Dapper;

namespace Alcedo.Core.Infrastructure.Dapper;

public class JsonTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    private static readonly JsonSerializerOptions Options = new();

    public override void SetValue(IDbDataParameter parameter, T? value)
    {
        parameter.Value = JsonSerializer.Serialize(value, Options);
    }

    public override T Parse(object value)
    {
        if (value is string json)
            return JsonSerializer.Deserialize<T>(json, Options)!;

        return JsonSerializer.Deserialize<T>(value.ToString()!, Options)!;
    }
}