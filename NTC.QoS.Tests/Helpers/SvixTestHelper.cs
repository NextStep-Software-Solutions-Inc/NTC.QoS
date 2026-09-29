using System.Security.Cryptography;
using System.Text;
using Svix;

namespace NTC.QoS.Tests.Helpers;

public static class SvixTestHelper
{
    // Valid 32-byte Base64 key prefixed with whsec_
    public const string TestSecret = "whsec_MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTIzNDU2Nzg5MDE=";

    public static (string SvixId, string SvixTimestamp, string SvixSignature) GenerateSvixHeaders(string payload, string secret = TestSecret)
    {
        var svixId = $"msg_{Guid.NewGuid():N}";
        var timestampSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var toSign = $"{svixId}.{timestampSeconds}.{payload}";

        var secretPart = secret.StartsWith("whsec_") ? secret[6..] : secret;
        var secretBytes = Convert.FromBase64String(secretPart);
        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(toSign));
        var signature = $"v1,{Convert.ToBase64String(hash)}";

        return (svixId, timestampSeconds, signature);
    }
}
