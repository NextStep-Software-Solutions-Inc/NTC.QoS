namespace NTC.QoS.Domain.Helper;

public static class StringHelper
{
    public static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var newText = text.Replace("dto", "", StringComparison.OrdinalIgnoreCase);
        return string.Concat(newText.Select((ch, i) =>
            i > 0 && char.IsUpper(ch) ? $" {ch}" : ch.ToString()));
    }

    /// <summary>
    /// Masks an email address to protect Personally Identifiable Information (PII) in log messages.
    /// Example: 'user.name@domain.gov.ph' becomes 'u***e@domain.gov.ph'.
    /// </summary>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "[empty]";
        }

        var atIndex = email.IndexOf('@');
        if (atIndex < 0)
        {
            return email.Length <= 2 ? "***" : $"{email[0]}***{email[^1]}";
        }

        var mailbox = email[..atIndex];
        var domain = email[atIndex..];

        if (mailbox.Length <= 1)
        {
            return $"*@{domain.TrimStart('@')}";
        }

        if (mailbox.Length == 2)
        {
            return $"{mailbox[0]}*@{domain.TrimStart('@')}";
        }

        return $"{mailbox[0]}***{mailbox[^1]}@{domain.TrimStart('@')}";
    }

    /// <summary>
    /// Sanitizes an error message or exception string by redacting potential authorization tokens or secrets.
    /// </summary>
    public static string SanitizeLogMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var sanitized = System.Text.RegularExpressions.Regex.Replace(
            message,
            @"(Bearer\s+)[A-Za-z0-9_\-\.]+",
            "$1[REDACTED]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"(sk_(?:live|test)_[A-Za-z0-9_]+)",
            "[REDACTED_SECRET]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"(whsec_[A-Za-z0-9_]+)",
            "[REDACTED_WEBHOOK_SECRET]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return sanitized;
    }
}
