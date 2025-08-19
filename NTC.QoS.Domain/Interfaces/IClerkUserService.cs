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

public interface IClerkUserService
{
    Task UpdateUserMetadataAsync(string userId, UserMetadataPayload payload);
    // Add other methods like GetUserAsync, CreateUserAsync, etc.
}