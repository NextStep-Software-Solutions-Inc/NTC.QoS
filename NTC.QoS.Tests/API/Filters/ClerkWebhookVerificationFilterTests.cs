using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NTC.QoS.API.Filters;
using NTC.QoS.Domain.Models;
using NTC.QoS.Tests.Helpers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace NTC.QoS.Tests.API.Filters;

public class ClerkWebhookVerificationFilterTests
{
    private readonly ClerkWebhookVerificationAttribute _filter = new();

    private static (ActionExecutingContext Context, ActionExecutionDelegate Next, Func<bool> NextCalled) CreateContext(
        string body,
        Dictionary<string, string?> configValues,
        Action<IHeaderDictionary>? configureHeaders = null)
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
        services.AddSingleton(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var bodyBytes = Encoding.UTF8.GetBytes(body);
        httpContext.Request.Body = new MemoryStream(bodyBytes);
        httpContext.Request.ContentType = "application/json";

        configureHeaders?.Invoke(httpContext.Request.Headers);

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var executingContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            new object());

        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, [], new object()));
        };

        return (executingContext, next, () => nextCalled);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenWebhookSecretNotConfigured_ReturnsBadRequest()
    {
        // Arrange
        var body = "{}";
        var (context, next, nextCalled) = CreateContext(body, new Dictionary<string, string?>());

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        context.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Webhook secret not configured");
        nextCalled().Should().BeFalse();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenSignatureIsMissing_ReturnsUnauthorized()
    {
        // Arrange
        var body = "{\"type\":\"user.created\"}";
        var config = new Dictionary<string, string?> { { "Clerk:WebhookSecret", SvixTestHelper.TestSecret } };
        var (context, next, nextCalled) = CreateContext(body, config);

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().Be("Invalid signature");
        nextCalled().Should().BeFalse();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenSignatureIsInvalid_ReturnsUnauthorized()
    {
        // Arrange
        var body = "{\"type\":\"user.created\"}";
        var config = new Dictionary<string, string?> { { "Clerk:WebhookSecret", SvixTestHelper.TestSecret } };
        var (context, next, nextCalled) = CreateContext(body, config, headers =>
        {
            headers["svix-id"] = "msg_123";
            headers["svix-timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            headers["svix-signature"] = "v1,invalid_signature_bytes==";
        });

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().Be("Invalid signature");
        nextCalled().Should().BeFalse();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenBodyIsTampered_ReturnsUnauthorized()
    {
        // Arrange
        var originalPayload = "{\"type\":\"user.created\",\"data\":{\"id\":\"user_orig\"}}";
        var (svixId, svixTimestamp, svixSignature) = SvixTestHelper.GenerateSvixHeaders(originalPayload);

        var tamperedPayload = "{\"type\":\"user.created\",\"data\":{\"id\":\"user_tampered\"}}";
        var config = new Dictionary<string, string?> { { "Clerk:WebhookSecret", SvixTestHelper.TestSecret } };
        var (context, next, nextCalled) = CreateContext(tamperedPayload, config, headers =>
        {
            headers["svix-id"] = svixId;
            headers["svix-timestamp"] = svixTimestamp;
            headers["svix-signature"] = svixSignature;
        });

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().Be("Invalid signature");
        nextCalled().Should().BeFalse();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenValidSignatureForUserCreated_DeserializesUserDataAndCallsNext()
    {
        // Arrange
        var payload = JsonSerializer.Serialize(new
        {
            type = "user.created",
            data = new
            {
                id = "user_clerk_valid_1",
                first_name = "Jane",
                last_name = "Doe",
                email_addresses = new[] { new { email_address = "jane@example.gov.ph" } }
            }
        });

        var (svixId, svixTimestamp, svixSignature) = SvixTestHelper.GenerateSvixHeaders(payload);
        var config = new Dictionary<string, string?> { { "Clerk:WebhookSecret", SvixTestHelper.TestSecret } };
        var (context, next, nextCalled) = CreateContext(payload, config, headers =>
        {
            headers["svix-id"] = svixId;
            headers["svix-timestamp"] = svixTimestamp;
            headers["svix-signature"] = svixSignature;
        });

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        context.Result.Should().BeNull();
        nextCalled().Should().BeTrue();

        var storedPayload = context.HttpContext.Items["ClerkWebhookPayload"] as ClerkWebhookPayload;
        storedPayload.Should().NotBeNull();
        storedPayload!.Type.Should().Be("user.created");
        storedPayload.Data.Should().BeOfType<ClerkUserData>();

        var userData = (ClerkUserData)storedPayload.Data!;
        userData.Id.Should().Be("user_clerk_valid_1");
        userData.FirstName.Should().Be("Jane");
        userData.LastName.Should().Be("Doe");
        userData.EmailAddresses.Should().ContainSingle(e => e.EmailAddress == "jane@example.gov.ph");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenValidSignatureForSessionCreated_DeserializesSessionDataAndCallsNext()
    {
        // Arrange
        var payload = JsonSerializer.Serialize(new
        {
            type = "session.created",
            data = new
            {
                id = "sess_12345",
                user_id = "user_clerk_session_1"
            }
        });

        var (svixId, svixTimestamp, svixSignature) = SvixTestHelper.GenerateSvixHeaders(payload);
        var config = new Dictionary<string, string?> { { "Clerk:WebhookSecret", SvixTestHelper.TestSecret } };
        var (context, next, nextCalled) = CreateContext(payload, config, headers =>
        {
            headers["svix-id"] = svixId;
            headers["svix-timestamp"] = svixTimestamp;
            headers["svix-signature"] = svixSignature;
        });

        // Act
        await _filter.OnActionExecutionAsync(context, next);

        // Assert
        context.Result.Should().BeNull();
        nextCalled().Should().BeTrue();

        var storedPayload = context.HttpContext.Items["ClerkWebhookPayload"] as ClerkWebhookPayload;
        storedPayload.Should().NotBeNull();
        storedPayload!.Type.Should().Be("session.created");
        storedPayload.Data.Should().BeOfType<ClerkSessionData>();

        var sessionData = (ClerkSessionData)storedPayload.Data!;
        sessionData.Id.Should().Be("sess_12345");
        sessionData.UserId.Should().Be("user_clerk_session_1");
    }
}
