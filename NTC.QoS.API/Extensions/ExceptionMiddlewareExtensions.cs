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
                    // Generate a correlation id to help trace logs for this error
                    var correlationId = Guid.NewGuid().ToString();
                    // Log full exception including stack trace and correlation id
                    logger.LogError(contextFeature.Error, "Unhandled exception (CorrelationId={CorrelationId})", correlationId);

                    // Determine environment to decide whether to expose exception details
                    var env = context.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

                    var errors = new Dictionary<string, string[]>();
                    // Always return correlation id to client so support can match logs
                    errors.Add("correlationId", new[] { correlationId });

                    // In development only, include the exception message for easier debugging
                    if (env != null && env.IsDevelopment())
                    {
                        errors.Add("exception", new[] { contextFeature.Error.Message });
                    }

                    var errorResponse = new ApiResponse<string>()
                    {
                        Data = null,
                        Message = "Internal Server Error. Please contact support.",
                        Succeeded = false,
                        Errors = errors
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
