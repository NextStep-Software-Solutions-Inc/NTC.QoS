using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTC.QoS.Domain.Interfaces;

// Define DTOs or command objects relevant to what Clerk API operations you need
public class UserMetadataPayload
{
    // It's generally better to use specific types if you know the structure.
    // If the metadata can truly be arbitrary, you might consider using
    // `IDictionary<string, object>` or `JsonElement` for more controlled deserialization,
    // but `object` is acceptable if you're directly serializing a dynamic object.
    public object? PublicMetadata { get; set; }
    public object? PrivateMetadata { get; set; }
}

public class CreateClerkUserPayload
{
    public string EmailAddress { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Password { get; set; }
    public object? PublicMetadata { get; set; }
    public object? PrivateMetadata { get; set; }
    public bool EmailVerified { get; set; } = true;
}

public class ClerkUserResponse
{
    public string Id { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public interface IClerkUserService
{
    Task UpdateUserMetadataAsync(string userId, UserMetadataPayload payload);
    Task<ClerkUserResponse> CreateUserAsync(CreateClerkUserPayload payload);
    // Add other methods like GetUserAsync, etc.
}