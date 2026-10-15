using System.Text.Json;
using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Interfaces;

public interface IClerkUserService
{
    Task UpdateUserMetadataAsync(string userId, UserMetadataPayload payload);
    Task<ClerkUserResponse> CreateUserAsync(CreateClerkUserPayload payload);
    Task<List<ClerkUserResponse>> GetUsersByEmailAsync(string email);
    Task<ClerkUserResponse?> GetUserByIdAsync(string userId);
    Task<ClerkInvitationResponse> CreateInvitationAsync(CreateClerkInvitationPayload payload);
    Task<List<ClerkInvitationResponse>> GetInvitationsAsync(string? status = null);
    Task<ClerkInvitationResponse> RevokeInvitationAsync(string invitationId);
}

// Define DTOs or command objects relevant to what Clerk API operations you need
public class CreateClerkInvitationPayload
{
    [JsonPropertyName("email_address")] public string EmailAddress { get; set; } = string.Empty;
    [JsonPropertyName("redirect_url")] public string? RedirectUrl { get; set; }
    [JsonPropertyName("public_metadata")] public object? PublicMetadata { get; set; }
    [JsonPropertyName("notify")] public bool Notify { get; set; } = true;
    [JsonPropertyName("ignore_existing")] public bool IgnoreExisting { get; set; } = true;
}

public class ClerkInvitationResponse
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("email_address")] public string EmailAddress { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("created_at")] public long? CreatedAt { get; set; }
    [JsonPropertyName("updated_at")] public long? UpdatedAt { get; set; }
    [JsonPropertyName("public_metadata")] public JsonElement? PublicMetadata { get; set; }
}

public class UserMetadataPayload
{
    [JsonPropertyName("public_metadata")] public object? PublicMetadata { get; set; }
    [JsonPropertyName("private_metadata")] public object? PrivateMetadata { get; set; }
}

public class CreateClerkUserPayload
{
    [JsonPropertyName("email_address")] public string EmailAddress { get; set; } = string.Empty;
    [JsonPropertyName("first_name")] public string? FirstName { get; set; }
    [JsonPropertyName("last_name")] public string? LastName { get; set; }
    [JsonPropertyName("password")] public string? Password { get; set; }
    [JsonPropertyName("public_metadata")] public object? PublicMetadata { get; set; }
    [JsonPropertyName("private_metadata")] public object? PrivateMetadata { get; set; }
}

public class ClerkEmailAddress
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("email_address")]
    public string EmailAddress { get; set; } = string.Empty;
}

public class ClerkUserResponse
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("email_addresses")] public List<ClerkEmailAddress> EmailAddresses { get; set; } = new();
    [JsonPropertyName("first_name")] public string? FirstName { get; set; }
    [JsonPropertyName("last_name")] public string? LastName { get; set; }
    [JsonPropertyName("public_metadata")] public JsonElement? PublicMetadata { get; set; }
}
