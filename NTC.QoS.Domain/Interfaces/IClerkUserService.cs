using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Interfaces;

public interface IClerkUserService
{
    Task UpdateUserMetadataAsync(string userId, UserMetadataPayload payload);
    Task<ClerkUserResponse> CreateUserAsync(CreateClerkUserPayload payload);
    Task<List<ClerkUserResponse>> GetUsersByEmailAsync(string email);
    Task<ClerkUserResponse?> GetUserByIdAsync(string userId);
}

// Define DTOs or command objects relevant to what Clerk API operations you need
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
}
