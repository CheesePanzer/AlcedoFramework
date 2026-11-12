using Alcedo.Core.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Alcedo.Core.Host;

/// <summary>
/// Fixes the issue where <see cref="ValueAtom"/> subtypes used as query parameters
/// are expanded by ApiExplorer into {Name}.Value.
/// Replaces the expanded parameter in-place with the correct string-typed parameter.
/// </summary>
internal sealed class ValueAtomParameterTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        operation.Parameters ??= new List<IOpenApiParameter>();

        var queryValueAtomParams = context.Description.ParameterDescriptions
            .Where(p => p.Source == BindingSource.Query
                        && p.ModelMetadata.ContainerType?.IsAssignableTo(typeof(ValueAtom)) == true);

        foreach (var apiParam in queryValueAtomParams)
        {
            var expanded = operation.Parameters
                .OfType<OpenApiParameter>()
                .FirstOrDefault(p => p.Name?.Equals(apiParam.Name, StringComparison.OrdinalIgnoreCase) == true
                                     && p.In == ParameterLocation.Query);

            if (expanded is null)
            {
                continue;
            }

            expanded.Name = apiParam.Name.EndsWith(".Value", StringComparison.OrdinalIgnoreCase)
                ? apiParam.Name[..^".Value".Length]
                : apiParam.Name;
            expanded.Schema = new OpenApiSchema { Type = JsonSchemaType.String };
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Fixes the issue where <see cref="ValueAtom"/> subtypes in response schemas
/// are expanded into objects instead of being treated as strings.
/// Forces the schema type to string.
/// </summary>
internal sealed class ValueAtomSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type.IsAssignableTo(typeof(ValueAtom)))
        {
            schema.Type = JsonSchemaType.String;
            schema.Properties?.Clear();
            schema.AllOf?.Clear();
            schema.OneOf?.Clear();
            schema.AnyOf?.Clear();
        }

        return Task.CompletedTask;
    }
}