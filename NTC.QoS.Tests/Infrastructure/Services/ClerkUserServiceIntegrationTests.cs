using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Infrastructure.Extensions;
using NTC.QoS.Infrastructure.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;

namespace NTC.QoS.Tests.Infrastructure.Services;

public class ClerkUserServiceIntegrationTests
{
    private readonly Mock<ILogger<ClerkUserService>> _loggerMock = new();

    private HttpClient CreateMockHttpClient(string secretKey, HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.clerk.com/v1/")
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secretKey);
        return client;
    }

    [Fact]
    public void AddExternalServices_ConfiguresHttpClient_WithBearerAuthorizationAndBaseAddress()
    {
        // Arrange
        const string expectedSecretKey = "sk_test_clerk_secret_key_12345";
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Clerk:SecretKey", expectedSecretKey },
            { "GeminiSettings:ApiKey", "dummy_key" }
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExternalServices(configuration);

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = clientFactory.CreateClient(typeof(IClerkUserService).Name);

        // Assert
        httpClient.BaseAddress.Should().Be(new Uri("https://api.clerk.com/v1/"));
        httpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
        httpClient.DefaultRequestHeaders.Authorization!.Parameter.Should().Be(expectedSecretKey);
        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task GetUsersByEmailAsync_SendsBearerAuthHeaderAndParsesUsers()
    {
        // Arrange
        const string secretKey = "sk_test_sample";
        const string searchEmail = "admin@ntc.gov.ph";
        HttpRequestMessage? capturedRequest = null;

        var mockResponse = new List<ClerkUserResponse>
        {
            new()
            {
                Id = "user_clerk_001",
                FirstName = "Admin",
                LastName = "NTC",
                EmailAddresses = [new() { Id = "email_01", EmailAddress = searchEmail }]
            }
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse))
            });

        var httpClient = CreateMockHttpClient(secretKey, handlerMock.Object);
        var service = new ClerkUserService(httpClient, _loggerMock.Object);

        // Act
        var result = await service.GetUsersByEmailAsync(searchEmail);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization!.Parameter.Should().Be(secretKey);
        capturedRequest.RequestUri!.ToString().Should().Contain("users?email_address=admin%40ntc.gov.ph");

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("user_clerk_001");
        result[0].EmailAddresses!.First().EmailAddress.Should().Be(searchEmail);
    }

    [Fact]
    public async Task GetUserByIdAsync_SendsBearerAuthHeaderAndParsesUser()
    {
        // Arrange
        const string secretKey = "sk_test_sample";
        const string userId = "user_clerk_002";
        HttpRequestMessage? capturedRequest = null;

        var mockResponse = new ClerkUserResponse
        {
            Id = userId,
            FirstName = "Super",
            LastName = "Admin"
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse))
            });

        var httpClient = CreateMockHttpClient(secretKey, handlerMock.Object);
        var service = new ClerkUserService(httpClient, _loggerMock.Object);

        // Act
        var result = await service.GetUserByIdAsync(userId);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization!.Parameter.Should().Be(secretKey);
        capturedRequest.RequestUri!.ToString().Should().EndWith($"users/{userId}");

        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.FirstName.Should().Be("Super");
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserNotFound_ReturnsNull()
    {
        // Arrange
        const string secretKey = "sk_test_sample";
        const string userId = "non_existent_user";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"errors\":[{\"message\":\"User not found\"}]}")
            });

        var httpClient = CreateMockHttpClient(secretKey, handlerMock.Object);
        var service = new ClerkUserService(httpClient, _loggerMock.Object);

        // Act
        var result = await service.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateUserAsync_SendsBearerAuthHeaderAndSerializesEmailArray()
    {
        // Arrange
        const string secretKey = "sk_test_sample";
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var payload = new CreateClerkUserPayload
        {
            EmailAddress = "newadmin@example.com",
            FirstName = "Admin",
            LastName = "User",
            Password = "SecurePassword123!",
            PublicMetadata = new Dictionary<string, object>
            {
                { "role", "super_admin" },
                { "organization", "national" }
            }
        };

        var mockResponse = new ClerkUserResponse
        {
            Id = "user_clerk_new_003",
            FirstName = "Admin",
            LastName = "User",
            EmailAddresses = [new() { Id = "email_03", EmailAddress = "newadmin@example.com" }]
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedRequest = req;
                if (req.Content != null)
                {
                    capturedBody = await req.Content.ReadAsStringAsync();
                }
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse))
            });

        var httpClient = CreateMockHttpClient(secretKey, handlerMock.Object);
        var service = new ClerkUserService(httpClient, _loggerMock.Object);

        // Act
        var result = await service.CreateUserAsync(payload);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization!.Parameter.Should().Be(secretKey);

        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("\"email_address\":[\"newadmin@example.com\"]");
        capturedBody.Should().Contain("\"role\":\"super_admin\"");

        result.Should().NotBeNull();
        result.Id.Should().Be("user_clerk_new_003");
    }

    [Fact]
    public async Task UpdateUserMetadataAsync_SendsBearerAuthHeaderAndPatchesPayload()
    {
        // Arrange
        const string secretKey = "sk_test_sample";
        const string userId = "user_clerk_update_004";
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var payload = new UserMetadataPayload
        {
            PublicMetadata = new Dictionary<string, object>
            {
                { "role", "super_admin" },
                { "organization", "national" },
                { "isDefaultAdmin", true }
            }
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedRequest = req;
                if (req.Content != null)
                {
                    capturedBody = await req.Content.ReadAsStringAsync();
                }
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\":\"user_clerk_update_004\"}")
            });

        var httpClient = CreateMockHttpClient(secretKey, handlerMock.Object);
        var service = new ClerkUserService(httpClient, _loggerMock.Object);

        // Act
        await service.UpdateUserMetadataAsync(userId, payload);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Patch);
        capturedRequest.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization!.Parameter.Should().Be(secretKey);
        capturedRequest.RequestUri!.ToString().Should().EndWith($"users/{userId}/metadata");

        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("\"role\":\"super_admin\"");
        capturedBody.Should().Contain("\"organization\":\"national\"");
        capturedBody.Should().Contain("\"isDefaultAdmin\":true");
    }

    [Fact]
    public async Task ClerkApi_WhenAuthenticationFails_ThrowsInvalidOperationExceptionWithDetails()
    {
        // Arrange
        const string invalidSecretKey = "invalid_key";
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"errors\":[{\"message\":\"Invalid authorization header format\",\"code\":\"authorization_header_format_invalid\"}],\"clerk_trace_id\":\"trace_123\"}")
            });

        var httpClient = CreateMockHttpClient(invalidSecretKey, handlerMock.Object);
        var service = new ClerkUserService(httpClient, _loggerMock.Object);

        // Act
        var act = () => service.GetUsersByEmailAsync("test@example.com");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*401*Unauthorized*");
    }
}
