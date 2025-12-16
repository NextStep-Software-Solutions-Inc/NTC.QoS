using Microsoft.Extensions.Logging;
using NTC.QoS.Domain.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NTC.QoS.Infrastructure.Services;

public class ClerkUserService(HttpClient httpClient, ILogger<ClerkUserService> logger) : IClerkUserService
{
    private static readonly JsonSerializerOptions RequestSerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions ResponseSerializerOptions = new()
    {
        PropertyNamingPolicy = null
    };

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<ClerkUserService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private sealed record ClerkApiErrorDetail([property: JsonPropertyName("message")] string Message,
                                               [property: JsonPropertyName("long_message")] string? LongMessage,
                                               [property: JsonPropertyName("code")] string? Code);

    private sealed record ClerkApiErrorResponse([property: JsonPropertyName("errors")] List<ClerkApiErrorDetail>? Errors,
                                                [property: JsonPropertyName("clerk_trace_id")] string? TraceId);

    public async Task<ClerkUserResponse> CreateUserAsync(CreateClerkUserPayload payload)
    {
        // The Clerk API expects email_address to be an array.
        // Construct a new object for serialization with email_address as an array.
        var requestObject = new
        {
            email_address = new[] { payload.EmailAddress },
            password = payload.Password,
            first_name = payload.FirstName,
            last_name = payload.LastName,
            public_metadata = payload.PublicMetadata,
            private_metadata = payload.PrivateMetadata
        };

        string jsonPayload = JsonSerializer.Serialize(requestObject, RequestSerializerOptions);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.PostAsync("users", content);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var parsed = TryParseError(responseBody);
            if (parsed != null)
            {
                foreach (var err in parsed.Errors ?? new())
                {
                    _logger.LogError("Clerk error code={Code} msg={Msg} detail={Detail} trace={Trace} status={Status}",
                        err.Code, err.Message, err.LongMessage, parsed.TraceId, response.StatusCode);
                }
            }
            else
            {
                _logger.LogError("Clerk create user failed. Status: {Status} RawBody: {Body}", response.StatusCode, responseBody);
            }
            
            throw new InvalidOperationException($"Clerk create user failed: {(int)response.StatusCode} {response.StatusCode}");
        }

        var userResponse = JsonSerializer.Deserialize<ClerkUserResponse>(responseBody, ResponseSerializerOptions);
        if (userResponse == null)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize Clerk user response. Status: {(int)response.StatusCode} {response.StatusCode}.");
        }

        _logger.LogInformation("Clerk user creation successful for user Id: {UserId}", userResponse.Id);
        return userResponse;
    }

    public async Task<List<ClerkUserResponse>> GetUsersByEmailAsync(string email)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"users?email_address={Uri.EscapeDataString(email)}");
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var parsed = TryParseError(responseBody);
            if (parsed != null)
            {
                foreach (var err in parsed.Errors ?? new())
                {
                    _logger.LogError("Clerk get users by email error code={Code} msg={Msg} detail={Detail} trace={Trace} status={Status} email={Email}",
                        err.Code, err.Message, err.LongMessage, parsed.TraceId, response.StatusCode, email);
                }
            }
            else
            {
                _logger.LogError("Clerk get users by email failed. Status: {Status} RawBody: {Body} Email: {Email}", response.StatusCode, responseBody, email);
            }
            
            throw new InvalidOperationException($"Clerk get users by email failed: {(int)response.StatusCode} {response.StatusCode}");
        }

        _logger.LogInformation("Clerk get users by email successful. Email: {Email} Response: {Response}", email, responseBody);
        var usersResponse = JsonSerializer.Deserialize<List<ClerkUserResponse>>(responseBody, ResponseSerializerOptions);
        return usersResponse ?? new List<ClerkUserResponse>();
    }

    private ClerkApiErrorResponse? TryParseError(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<ClerkApiErrorResponse>(body, ResponseSerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize Clerk API error response. Body: {Body}", body);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Clerk API error response deserialization. Body: {Body}", body);
            return null;
        }
    }

    public async Task UpdateUserMetadataAsync(string userId, UserMetadataPayload payload)
    {
        string jsonPayload = JsonSerializer.Serialize(payload, RequestSerializerOptions);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.PatchAsync($"users/{userId}/metadata", content);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var parsed = TryParseError(responseBody);
            if (parsed != null)
            {
                foreach (var err in parsed.Errors ?? new())
                {
                    _logger.LogError("Clerk metadata update error user={UserId} code={Code} msg={Msg} detail={Detail} trace={Trace} status={Status} payload={Payload}",
                        userId, err.Code, err.Message, err.LongMessage, parsed.TraceId, response.StatusCode, jsonPayload);
                }
            }
            else
            {
                _logger.LogError("Clerk update metadata failed for {UserId}. Status: {Status} Body: {Body} Payload: {Payload}", userId, response.StatusCode, responseBody, jsonPayload);
            }
            throw new InvalidOperationException($"Clerk update metadata failed for {userId}: {(int)response.StatusCode} {response.StatusCode}");
        }

        _logger.LogInformation("Clerk metadata update successful for user {UserId}. Response: {Response}", userId, responseBody);
    }
}