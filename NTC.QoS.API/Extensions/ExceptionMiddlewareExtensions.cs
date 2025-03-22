using Microsoft.AspNetCore.Diagnostics;
using NTC.QoS.Domain.Models.DTOs;
using System.Text.Json;

namespace NTC.QoS.API.Extensions;
public static class ExceptionMiddlewareExtensions
{
    public static void ConfigureExceptionHandler(this IApplicationBuilder app, ILogger logger)
    {
        _ = app.UseExceptionHandler(appError =>
        {
            appError.Run(async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (contextFeature != null)
                {
                    logger.LogError("Something went wrong: {Error}", contextFeature.Error);

                    var errorResponse = new ApiResponse<string>()
                    {
                        Data = null,
                        Message = "Internal Server Error. Please contact support.",
                        Succeeded = false,
                        Error = [ contextFeature.Error.Message ]
                    };

                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
                }
            });
        });
    }
}
