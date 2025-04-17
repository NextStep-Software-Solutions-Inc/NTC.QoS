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
}
