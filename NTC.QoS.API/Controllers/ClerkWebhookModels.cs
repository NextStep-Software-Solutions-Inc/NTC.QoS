namespace NTC.QoS.API.Controllers;

public class ClerkWebhookPayload
{
    public required string Type { get; set; }
    public required ClerkUserData Data { get; set; }
}

public class ClerkUserData
{
    public required string Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required List<ClerkEmailAddress> EmailAddresses { get; set; } = new();
}

public class ClerkEmailAddress
{
    public required string EmailAddress { get; set; }
}