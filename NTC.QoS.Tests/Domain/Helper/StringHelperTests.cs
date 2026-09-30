using NTC.QoS.Domain.Helper;
using Xunit;

namespace NTC.QoS.Tests.Domain.Helper;

public class StringHelperTests
{
    [Theory]
    [InlineData("user.name@domain.gov.ph", "u***e@domain.gov.ph")]
    [InlineData("john.doe@ntc.gov.ph", "j***e@ntc.gov.ph")]
    [InlineData("ab@example.com", "a*@example.com")]
    [InlineData("a@example.com", "*@example.com")]
    [InlineData(null, "[empty]")]
    [InlineData("", "[empty]")]
    [InlineData("   ", "[empty]")]
    [InlineData("plainstring", "p***g")]
    [InlineData("ab", "***")]
    public void MaskEmail_ReturnsMaskedString(string? input, string expected)
    {
        var result = StringHelper.MaskEmail(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeLogMessage_RedactsSensitiveTokens()
    {
        var sampleKey = "sk_" + "test_" + "mocksecretkeyvalue12345";
        var sampleWebhookSecret = "whsec_" + "mockwebhooksecretvalue12345";
        var inputWithBearer = "Authorization: Bearer mock_bearer_token_12345";
        var inputWithKey = $"Failed with key {sampleKey}";
        var inputWithSecret = $"Webhook secret {sampleWebhookSecret} failed validation";

        Assert.Equal("Authorization: Bearer [REDACTED]", StringHelper.SanitizeLogMessage(inputWithBearer));
        Assert.Equal("Failed with key [REDACTED_SECRET]", StringHelper.SanitizeLogMessage(inputWithKey));
        Assert.Equal("Webhook secret [REDACTED_WEBHOOK_SECRET] failed validation", StringHelper.SanitizeLogMessage(inputWithSecret));
        Assert.Equal("Normal error message with no secrets", StringHelper.SanitizeLogMessage("Normal error message with no secrets"));
        Assert.Equal(string.Empty, StringHelper.SanitizeLogMessage(null));
        Assert.Equal(string.Empty, StringHelper.SanitizeLogMessage(string.Empty));
    }

    [Theory]
    [InlineData("RoleDto", "Role")]
    [InlineData("UserDto", "User")]
    [InlineData("CreateUserDto", "Create User")]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void NormalizeText_ReturnsFormattedText(string? input, string? expected)
    {
        var result = StringHelper.NormalizeText(input!);
        Assert.Equal(expected, result);
    }
}
