using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Svix;
using System.Net;
using System.Text.Json;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Models;

namespace NTC.QoS.API.Filters;

public class ClerkWebhookVerificationAttribute : ActionFilterAttribute, IAsyncActionFilter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true
    };
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var logger = context.HttpContext.RequestServices.GetService<ILogger<ClerkWebhookVerificationAttribute>>();
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var webhookSecret = configuration["Clerk:WebhookSecret"];
        if (string.IsNullOrEmpty(webhookSecret))
        {
            logger?.LogError("Clerk webhook verification failed: Webhook secret not configured in application settings.");
            context.Result = new BadRequestObjectResult("Webhook secret not configured");
            return;
        }

        var request = context.HttpContext.Request;
        request.EnableBuffering();

        string body;
        using (var reader = new StreamReader(request.Body, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
        }

        // Reset the stream position for the action to read it again
        request.Body.Position = 0;

        var headers = request.Headers;
        var svixId = headers["svix-id"].ToString();
        var svixTimestamp = headers["svix-timestamp"].ToString();
        var svixSignature = headers["svix-signature"].ToString();

        if (string.IsNullOrWhiteSpace(svixId) || string.IsNullOrWhiteSpace(svixTimestamp) || string.IsNullOrWhiteSpace(svixSignature))
        {
            logger?.LogWarning("Clerk webhook verification rejected: Missing required Svix headers (svix-id={SvixId}, svix-timestamp={SvixTimestamp})", svixId, svixTimestamp);
            context.Result = new UnauthorizedObjectResult("Invalid signature");
            return;
        }

        var svixHeaders = new WebHeaderCollection
        {
            ["svix-id"] = svixId,
            ["svix-timestamp"] = svixTimestamp,
            ["svix-signature"] = svixSignature
        };

        var wh = new Webhook(webhookSecret);
        try
        {
            wh.Verify(body, svixHeaders);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Clerk webhook verification failed for svix-id {SvixId}: {Message}", svixId, StringHelper.SanitizeLogMessage(ex.Message));
            context.Result = new UnauthorizedObjectResult("Invalid signature");
            return;
        }

        // Store the payload in HttpContext for the action to use
        var payload = JsonSerializer.Deserialize<ClerkWebhookPayload>(body, SerializerOptions);
        if (payload == null)
        {
            context.Result = new BadRequestObjectResult("Invalid payload");
            return;
        }

        // Deserialize Data based on Type
        if (payload.Type.StartsWith("user.") && payload.Data is JsonElement userElement)
        {
            var dataJson = userElement.GetRawText();
            var userData = JsonSerializer.Deserialize<ClerkUserData>(dataJson, SerializerOptions);
            if (userData == null)
            {
                context.Result = new BadRequestObjectResult("Invalid user data in payload");
                return;
            }
            payload.Data = userData;
        }
        else if (payload.Type.StartsWith("session.") && payload.Data is JsonElement sessionElement)
        {
            var dataJson = sessionElement.GetRawText();
            var sessionData = JsonSerializer.Deserialize<ClerkSessionData>(dataJson, SerializerOptions);
            if (sessionData == null)
            {
                context.Result = new BadRequestObjectResult("Invalid session data in payload");
                return;
            }
            payload.Data = sessionData;
        }
        // For other types, leave as is

        context.HttpContext.Items["ClerkWebhookPayload"] = payload;

        await next();
    }
}