using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Alcedo.Core.Host;

public class NumberTypeSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken token)
    {
        if (context.ParameterDescription != null) //skip input param
        {
            return Task.CompletedTask;
        }

        switch (schema.Format)
        {
            case "int32":
            case "int64":
                schema.Type = JsonSchemaType.Integer;
                schema.Pattern = null;
                break;
            case "float":
            case "double":
            case "decimal":
                schema.Type = JsonSchemaType.Number;
                schema.Pattern = null;
                break;
        }

        return Task.CompletedTask;
    }
}