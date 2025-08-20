using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;
using System.Reflection;

namespace NTC.QoS.API.Filters;
public class CamelCaseQueryParamsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
            return;

        foreach (var param in operation.Parameters)
        {
            var paramDesc = context.ApiDescription.ParameterDescriptions
                .FirstOrDefault(p => p.Name == param.Name);
            if (paramDesc?.ModelMetadata?.ContainerType != null && paramDesc.ModelMetadata.PropertyName != null)
            {
                var prop = paramDesc.ModelMetadata.ContainerType.GetProperty(paramDesc.ModelMetadata.PropertyName);
                if (prop != null)
                {
                    var jsonProp = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
                    if (jsonProp != null)
                    {
                        param.Name = jsonProp.Name;
                    }
                }
            }
        }
    }
}
