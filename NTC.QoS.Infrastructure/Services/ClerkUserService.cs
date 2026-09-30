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

        _logger.LogInformation("Clerk get users by email successful for {Email}", email);
        var usersResponse = JsonSerializer.Deserialize<List<ClerkUserResponse>>(responseBody, ResponseSerializerOptions);
        return usersResponse ?? new List<ClerkUserResponse>();
    }

    public async Task<ClerkUserResponse?> GetUserByIdAsync(string userId)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"users/{userId}");
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Clerk user not found. UserId: {UserId}", userId);
                return null;
            }

            var parsed = TryParseError(responseBody);
            if (parsed != null)
            {
                foreach (var err in parsed.Errors ?? new())
                {
                    _logger.LogError("Clerk get user by id error user={UserId} code={Code} msg={Msg} detail={Detail} trace={Trace} status={Status}",
                        userId, err.Code, err.Message, err.LongMessage, parsed.TraceId, response.StatusCode);
                }
            }
            else
            {
                _logger.LogError("Clerk get user by id failed. UserId: {UserId} Status: {Status}", userId, response.StatusCode);
            }
            
            throw new InvalidOperationException($"Clerk get user by id failed: {(int)response.StatusCode} {response.StatusCode}");
        }

        _logger.LogInformation("Clerk get user by id successful for {UserId}", userId);
        var userResponse = JsonSerializer.Deserialize<ClerkUserResponse>(responseBody, ResponseSerializerOptions);
        return userResponse;
    }

    private ClerkApiErrorResponse? TryParseError(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<ClerkApiErrorResponse>(body, ResponseSerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize Clerk API error response");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Clerk API error response deserialization");
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
                    _logger.LogError("Clerk metadata update error user={UserId} code={Code} msg={Msg} detail={Detail} trace={Trace} status={Status}",
                        userId, err.Code, err.Message, err.LongMessage, parsed.TraceId, response.StatusCode);
                }
            }
            else
            {
                _logger.LogError("Clerk update metadata failed for {UserId}. Status: {Status}", userId, response.StatusCode);
            }
            throw new InvalidOperationException($"Clerk update metadata failed for {userId}: {(int)response.StatusCode} {response.StatusCode}");
        }

        _logger.LogInformation("Clerk metadata update successful for user {UserId}", userId);
    }

    public async Task<ClerkInvitationResponse> CreateInvitationAsync(CreateClerkInvitationPayload payload)
    {
        var requestObject = new
        {
            email_address = payload.EmailAddress,
            redirect_url = payload.RedirectUrl,
            public_metadata = payload.PublicMetadata,
            notify = payload.Notify,
            ignore_existing = payload.IgnoreExisting
        };

        string jsonPayload = JsonSerializer.Serialize(requestObject, RequestSerializerOptions);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.PostAsync("invitations", content);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var parsed = TryParseError(responseBody);
            if (parsed != null)
            {
                foreach (var err in parsed.Errors ?? new())
                {
                    _logger.LogError("Clerk create invitation error code={Code} msg={Msg} detail={Detail} trace={Trace} status={Status} email={Email}",
                        err.Code, err.Message, err.LongMessage, parsed.TraceId, response.StatusCode, payload.EmailAddress);
                }

                var firstError = parsed.Errors?.FirstOrDefault()?.Message ?? "Failed to create invitation in Clerk.";
                throw new InvalidOperationException(firstError);
            }

            _logger.LogError("Clerk create invitation failed. Status: {Status} Body: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Clerk create invitation failed: {(int)response.StatusCode} {response.StatusCode}");
        }

        _logger.LogInformation("Clerk invitation created successfully for {Email}", payload.EmailAddress);
        var invitationResponse = JsonSerializer.Deserialize<ClerkInvitationResponse>(responseBody, ResponseSerializerOptions);
        return invitationResponse ?? throw new InvalidOperationException("Failed to deserialize Clerk invitation response.");
    }

    public async Task<List<ClerkInvitationResponse>> GetInvitationsAsync(string? status = null)
    {
        string endpoint = string.IsNullOrWhiteSpace(status) ? "invitations" : $"invitations?status={Uri.EscapeDataString(status)}";
        HttpResponseMessage response = await _httpClient.GetAsync(endpoint);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var parsed = TryParseError(responseBody);
            if (parsed != null)
            {
                foreach (var err in parsed.Errors ?? new())
                {
                    _logger.LogError("Clerk get invitations error code={Code} msg={Msg} detail={Detail} trace={Trace} status={Status}",
                        err.Code, err.Message, err.LongMessage, parsed.TraceId, response.StatusCode);
                }

                var firstError = parsed.Errors?.FirstOrDefault()?.Message ?? "Failed to retrieve invitations from Clerk.";
                throw new InvalidOperationException(firstError);
            }

            _logger.LogError("Clerk get invitations failed. Status: {Status} Body: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Clerk get invitations failed: {(int)response.StatusCode} {response.StatusCode}");
        }

        var invitations = JsonSerializer.Deserialize<List<ClerkInvitationResponse>>(responseBody, ResponseSerializerOptions);
        return invitations ?? new List<ClerkInvitationResponse>();
    }

    public async Task<ClerkInvitationResponse> RevokeInvitationAsync(string invitationId)
    {
        HttpResponseMessage response = await _httpClient.PostAsync($"invitations/{Uri.EscapeDataString(invitationId)}/revoke", null);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var parsed = TryParseError(responseBody);
            if (parsed != null)
            {
                foreach (var err in parsed.Errors ?? new())
                {
                    _logger.LogError("Clerk revoke invitation error code={Code} msg={Msg} detail={Detail} trace={Trace} status={Status} id={Id}",
                        err.Code, err.Message, err.LongMessage, parsed.TraceId, response.StatusCode, invitationId);
                }

                var firstError = parsed.Errors?.FirstOrDefault()?.Message ?? "Failed to revoke invitation in Clerk.";
                throw new InvalidOperationException(firstError);
            }

            _logger.LogError("Clerk revoke invitation failed. Status: {Status} Body: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Clerk revoke invitation failed: {(int)response.StatusCode} {response.StatusCode}");
        }

        _logger.LogInformation("Clerk invitation revoked successfully for {InvitationId}", invitationId);
        var invitationResponse = JsonSerializer.Deserialize<ClerkInvitationResponse>(responseBody, ResponseSerializerOptions);
        return invitationResponse ?? throw new InvalidOperationException("Failed to deserialize revoked invitation response.");
    }
}