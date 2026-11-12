using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Alcedo.Core.Host;

internal class EnumDisplayStringTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type.IsEnum)
        {
            schema.Type = JsonSchemaType.String;
        }

        return Task.CompletedTask;
    }
}