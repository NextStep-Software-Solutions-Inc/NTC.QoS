using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models;

public class ClerkWebhookPayload
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }
    [JsonPropertyName("data")]
    public required ClerkUserData Data { get; set; }
}

public class ClerkUserData
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    [JsonPropertyName("first_name")]
    public required string FirstName { get; set; }
    [JsonPropertyName("last_name")]
    public required string LastName { get; set; }
    [JsonPropertyName("email_addresses")]
    public required List<ClerkEmailAddress> EmailAddresses { get; set; } = [];
}

public class ClerkEmailAddress
{
    [JsonPropertyName("email_address")]
    public required string EmailAddress { get; set; }
}