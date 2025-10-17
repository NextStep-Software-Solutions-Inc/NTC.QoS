using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NTC.QoS.API.Filters;

/// <summary>
/// Operation filter to transform all schema property keys to camelCase
/// </summary>
public class CamelCaseOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Only transform keys for form data request bodies
        if (operation.RequestBody?.Content != null)
        {
            var hasFormData = operation.RequestBody.Content.ContainsKey("multipart/form-data") ||
                              operation.RequestBody.Content.ContainsKey("application/x-www-form-urlencoded");

            if (hasFormData)
            {
                foreach (var mediaType in operation.RequestBody.Content.Values)
                {
                    if (mediaType.Schema != null)
                    {
                        TransformSchemaToCamelCase(mediaType.Schema);
                    }
                }
            }
        }

        // Optionally, transform response schemas if needed (commented out for now)
        // if (operation.Responses != null)
        // {
        //     foreach (var response in operation.Responses.Values)
        //     {
        //         if (response.Content != null)
        //         {
        //             foreach (var mediaType in response.Content.Values)
        //             {
        //                 if (mediaType.Schema != null)
        //                 {
        //                     TransformSchemaToCamelCase(mediaType.Schema);
        //                 }
        //             }
        //         }
        //     }
        // }
    }

    private static void TransformSchemaToCamelCase(OpenApiSchema schema)
    {
        if (schema.Properties != null && schema.Properties.Count > 0)
        {
            var transformedProperties = new Dictionary<string, OpenApiSchema>();

            foreach (var property in schema.Properties)
            {
                var camelCaseKey = ToCamelCase(property.Key);
                transformedProperties[camelCaseKey] = property.Value;
            }

            schema.Properties = transformedProperties;
        }

        // Recursively transform nested schemas
        if (schema.Items != null)
        {
            TransformSchemaToCamelCase(schema.Items);
        }

        if (schema.AllOf != null)
        {
            foreach (var subSchema in schema.AllOf)
            {
                TransformSchemaToCamelCase(subSchema);
            }
        }

        if (schema.OneOf != null)
        {
            foreach (var subSchema in schema.OneOf)
            {
                TransformSchemaToCamelCase(subSchema);
            }
        }

        if (schema.AnyOf != null)
        {
            foreach (var subSchema in schema.AnyOf)
            {
                TransformSchemaToCamelCase(subSchema);
            }
        }

        if (schema.Not != null)
        {
            TransformSchemaToCamelCase(schema.Not);
        }

        if (schema.AdditionalProperties != null && schema.AdditionalProperties is OpenApiSchema additionalSchema)
        {
            TransformSchemaToCamelCase(additionalSchema);
        }
    }

    private static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToLowerInvariant(input[0]) + input[1..];
    }
}