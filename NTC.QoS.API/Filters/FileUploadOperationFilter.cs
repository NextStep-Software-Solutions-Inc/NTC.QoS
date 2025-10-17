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

        var parameters = context.MethodInfo.GetParameters();

        foreach (var parameter in parameters)
        {
            // Handle parameters marked with [FromForm] or complex types that contain IFormFile
            if (parameter.GetCustomAttribute<FromFormAttribute>() != null || HasFormFileProperty(parameter.ParameterType))
            {
                if (parameter.ParameterType == typeof(IFormFile))
                {
                    var parameterName = GetParameterName(parameter);
                    schema.Properties[parameterName] = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    };
                }
                else if (parameter.ParameterType == typeof(string))
                {
                    var parameterName = GetParameterName(parameter);
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
            else if (property.PropertyType == typeof(string))
            {
                schema.Properties[propertyName] = new OpenApiSchema
                {
                    Type = "string",
                    Nullable = IsNullable(property)
                };
            }
            else if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
            {
                schema.Properties[propertyName] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "uuid",
                    Nullable = property.PropertyType == typeof(Guid?)
                };
            }
            else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
            {
                schema.Properties[propertyName] = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int32",
                    Nullable = property.PropertyType == typeof(int?)
                };
            }
            else if (property.PropertyType == typeof(long) || property.PropertyType == typeof(long?))
            {
                schema.Properties[propertyName] = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int64",
                    Nullable = property.PropertyType == typeof(long?)
                };
            }
            else if (property.PropertyType == typeof(double) || property.PropertyType == typeof(double?))
            {
                schema.Properties[propertyName] = new OpenApiSchema
                {
                    Type = "number",
                    Format = "double",
                    Nullable = property.PropertyType == typeof(double?)
                };
            }
            else if (property.PropertyType == typeof(float) || property.PropertyType == typeof(float?))
            {
                schema.Properties[propertyName] = new OpenApiSchema
                {
                    Type = "number",
                    Format = "float",
                    Nullable = property.PropertyType == typeof(float?)
                };
            }
            else if (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?))
            {
                schema.Properties[propertyName] = new OpenApiSchema
                {
                    Type = "number",
                    Format = "decimal",
                    Nullable = property.PropertyType == typeof(decimal?)
                };
            }
            else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                schema.Properties[propertyName] = new OpenApiSchema
                {
                    Type = "boolean",
                    Nullable = property.PropertyType == typeof(bool?)
                };
            }
            else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
            {
                schema.Properties[propertyName] = new OpenApiSchema
                {
                    Type = "string",
                    Format = "date-time",
                    Nullable = property.PropertyType == typeof(DateTime?)
                };
            }
            else if (property.PropertyType.IsEnum)
            {
                schema.Properties[propertyName] = new OpenApiSchema
                {
                    Type = "string",
                    Enum = Enum.GetNames(property.PropertyType).Select(name => (Microsoft.OpenApi.Any.IOpenApiAny)new Microsoft.OpenApi.Any.OpenApiString(name)).ToList()
                };
            }
        }

        return schema;
    }

    private static bool IsNullable(PropertyInfo property)
    {
        // Check if the property type is nullable reference type or nullable value type
        return !property.PropertyType.IsValueType || 
               Nullable.GetUnderlyingType(property.PropertyType) != null;
    }

    private static string GetParameterName(ParameterInfo parameter)
    {
        var fromFormAttribute = parameter.GetCustomAttribute<FromFormAttribute>();
        return fromFormAttribute?.Name ?? parameter.Name ?? "parameter";
    }
}