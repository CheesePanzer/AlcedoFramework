using System.Text.Json.Nodes;
using Alcedo.Core.Presentation.Filters;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Alcedo.Core.Host;

public class IdempotencyKeyTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        #region This Section Should Be Identical to the IdempotencyKeyFilter

        var httpMethod = context.Description.HttpMethod ?? "GET";

        if (httpMethod == "GET")
        {
            return Task.CompletedTask;
        }

        var metaData = context.Description.ActionDescriptor.EndpointMetadata;
        var strictAttr = metaData.OfType<StrictIdempotencyKeyAttribute>().FirstOrDefault();
        var softKeyAttr = metaData.OfType<SoftIdempotencyKeyAttribute>().FirstOrDefault();

        if (httpMethod is "PUT" or "PATCH" or "DELETE" && strictAttr is null && softKeyAttr is null)
        {
            return Task.CompletedTask;
        }

        #endregion

        operation.Parameters ??= new List<IOpenApiParameter>();
        if (operation.Parameters.All(p => p.Name != "x-idempotency-key"))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "x-idempotency-key",
                In = ParameterLocation.Header,
                Required = true,
                Description = "Idempotency-Key",
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Format = "uuid",
                    Example = JsonValue.Create(Guid.NewGuid().ToString())
                }
            });
        }

        return Task.CompletedTask;
    }
}