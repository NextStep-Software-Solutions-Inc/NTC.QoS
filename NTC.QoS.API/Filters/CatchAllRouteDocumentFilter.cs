using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NTC.QoS.API.Filters;

public class CatchAllRouteDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var path in swaggerDoc.Paths.ToList())
        {
            var key = path.Key;

            // Find paths with the catch-all parameter syntax
            if (key.Contains("{**"))
            {
                // Replace the double-star syntax with single to render correctly in Swagger UI
                var newKey = key.Replace("{**", "{");
                swaggerDoc.Paths.Remove(key);
                swaggerDoc.Paths.Add(newKey, path.Value);

                // Optionally, improve the description for the UI
                var param = path.Value.Operations.Values.FirstOrDefault()?.Parameters.FirstOrDefault();
                if (param != null)
                {
                    param.Description = "This is a catch-all route parameter that can contain slashes.";
                }
            }
        }
    }
}
