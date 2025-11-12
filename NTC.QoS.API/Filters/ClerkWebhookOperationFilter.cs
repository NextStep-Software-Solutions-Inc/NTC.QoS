using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace NTC.QoS.API.Filters;

/// <summary>
/// Custom operation filter to handle ClerkWebhookVerification attribute in Swagger documentation
/// </summary>
public class ClerkWebhookOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasWebhookVerification = context.MethodInfo.GetCustomAttributes()
            .Any(attr => attr.GetType() == typeof(ClerkWebhookVerificationAttribute));

        if (hasWebhookVerification)
        {
            // Add webhook-specific information to the operation
            operation.Summary = "Clerk Webhook Endpoint";
            operation.Description = "Receives authenticated webhook events from Clerk authentication service. " +
                                  "The request is automatically verified using Svix signature verification.";

            // Add webhook security scheme
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "WebhookSignature"
                            }
                        },
                        Array.Empty<string>()
                    }
                }
            ];

            // Add example request body
            if (operation.RequestBody == null)
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Description = "Clerk webhook payload",
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["type"] = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Description = "Webhook event type (e.g., 'user.created', 'user.updated')"
                                    },
                                    ["data"] = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Description = "User data payload",
                                        Properties = new Dictionary<string, OpenApiSchema>
                                        {
                                            ["id"] = new OpenApiSchema
                                            {
                                                Type = "string",
                                                Description = "Clerk user ID"
                                            },
                                            ["firstName"] = new OpenApiSchema
                                            {
                                                Type = "string",
                                                Description = "User's first name"
                                            },
                                            ["lastName"] = new OpenApiSchema
                                            {
                                                Type = "string",
                                                Description = "User's last name"
                                            },
                                            ["emailAddresses"] = new OpenApiSchema
                                            {
                                                Type = "array",
                                                Items = new OpenApiSchema
                                                {
                                                    Type = "object",
                                                    Properties = new Dictionary<string, OpenApiSchema>
                                                    {
                                                        ["emailAddress"] = new OpenApiSchema
                                                        {
                                                            Type = "string",
                                                            Description = "User's email address"
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
            }
        }
    }
}