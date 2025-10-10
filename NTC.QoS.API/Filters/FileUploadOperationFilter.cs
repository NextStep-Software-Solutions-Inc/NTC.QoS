using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace NTC.QoS.API.Filters;

/// <summary>
/// Operation filter to handle file upload endpoints for Swagger documentation
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasFormFileParameter = context.MethodInfo.GetParameters()
            .Any(p => p.ParameterType == typeof(IFormFile) || 
                     p.ParameterType == typeof(IEnumerable<IFormFile>) ||
                     p.ParameterType == typeof(IFormFileCollection) ||
                     HasFormFileProperty(p.ParameterType));

        if (!hasFormFileParameter)
            return;

        // Clear existing parameters that might cause conflicts
        operation.Parameters?.Clear();

        // Set request body for multipart/form-data
        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = GenerateSchemaForFormData(context)
                }
            }
        };
    }

    private static bool HasFormFileProperty(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type.IsEnum)
            return false;

        return type.GetProperties()
            .Any(p => p.PropertyType == typeof(IFormFile) || 
                     p.PropertyType == typeof(IEnumerable<IFormFile>) ||
                     p.PropertyType == typeof(IFormFileCollection));
    }

    private static OpenApiSchema GenerateSchemaForFormData(OperationFilterContext context)
    {
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>()
        };

        var parameters = context.MethodInfo.GetParameters()
            .Where(p => p.GetCustomAttribute<FromFormAttribute>() != null);

        foreach (var parameter in parameters)
        {
            var parameterName = GetParameterName(parameter);
            
            if (parameter.ParameterType == typeof(IFormFile))
            {
                schema.Properties[parameterName] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                };
            }
            else if (parameter.ParameterType == typeof(string))
            {
                schema.Properties[parameterName] = new OpenApiSchema
                {
                    Type = "string"
                };
            }
            else if (HasFormFileProperty(parameter.ParameterType))
            {
                // Handle complex types with IFormFile properties
                var complexSchema = GenerateSchemaForComplexType(parameter.ParameterType);
                foreach (var prop in complexSchema.Properties)
                {
                    schema.Properties[prop.Key] = prop.Value;
                }
            }
        }

        return schema;
    }

    private static OpenApiSchema GenerateSchemaForComplexType(Type type)
    {
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>()
        };

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var propertyName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];

            if (property.PropertyType == typeof(IFormFile))
            {
                schema.Properties[propertyName] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                };
            }
            else if (property.PropertyType == typeof(string) || property.PropertyType.Name == "String")
            {
                schema.Properties[propertyName] = new OpenApiSchema
                {
                    Type = "string",
                    Nullable = !property.PropertyType.IsValueType
                };
            }
        }

        return schema;
    }

    private static string GetParameterName(ParameterInfo parameter)
    {
        var fromFormAttribute = parameter.GetCustomAttribute<FromFormAttribute>();
        return fromFormAttribute?.Name ?? parameter.Name ?? "parameter";
    }
}