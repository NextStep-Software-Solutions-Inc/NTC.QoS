using System.Security.Cryptography;
using System.Text;
using Svix;

namespace NTC.QoS.Tests.Helpers;

public static class SvixTestHelper
{
    // Valid 32-byte Base64 key prefixed with whsec_ generated dynamically at runtime for test fixtures
    public static readonly string TestSecret = "whsec_" + Convert.ToBase64String(Encoding.UTF8.GetBytes("01234567890123456789012345678901"));

    public static (string SvixId, string SvixTimestamp, string SvixSignature) GenerateSvixHeaders(string payload, string? secret = null, DateTimeOffset? timestamp = null)
    {
        var effectiveSecret = secret ?? TestSecret;
        var svixId = $"msg_{Guid.NewGuid():N}";
        var timestampSeconds = (timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds().ToString();
        var toSign = $"{svixId}.{timestampSeconds}.{payload}";

        var secretPart = effectiveSecret.StartsWith("whsec_") ? effectiveSecret[6..] : effectiveSecret;
        var secretBytes = Convert.FromBase64String(secretPart);
        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(toSign));
        var signature = $"v1,{Convert.ToBase64String(hash)}";

        return (svixId, timestampSeconds, signature);
    }
}
