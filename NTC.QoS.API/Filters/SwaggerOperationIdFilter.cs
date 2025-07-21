using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NTC.QoS.API.Filters;

public class SwaggerOperationIdFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if it's a MapIdentityApi action
        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor)
        {
            var relativePath = context.ApiDescription.RelativePath ?? "";
            operation.OperationId = $"{GenerateOperationIdBaseString(relativePath)}_{context.ApiDescription.HttpMethod}";
            return;
        }

        // For other controller actions, set OperationId based on controller and action names
        operation.OperationId = $"{controllerActionDescriptor.ControllerName}_{controllerActionDescriptor.ActionName}_{context.ApiDescription.HttpMethod}";
    }

    // Helper Method to extract the controller and the action name
    private static string GenerateOperationIdBaseString(string inputString)
    {
        var input = inputString.Split('/');
        if (input.Length == 0)
        {
            return string.Empty;
        }

        var controllerName = input[0].FirstCharToUpper();
        var actionName = string.Join("", input.Skip(1).Select(word => word.FirstCharToUpper()));

        return $"{controllerName}_{actionName}";
    }
}

public static class StringExtensions
{
    public static string FirstCharToUpper(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return string.Concat(input.First().ToString().ToUpper(System.Globalization.CultureInfo.CurrentCulture), input.AsSpan(1));
    }
}

