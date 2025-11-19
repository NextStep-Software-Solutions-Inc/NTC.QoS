using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;

namespace NTC.QoS.Infrastructure.Data.Helpers;

/// <summary>
/// Helper class for fuzzy matching location names from Excel data to database entities
/// </summary>
public static class NameMatcher
{
    /// <summary>
    /// Finds the best match for a name from a collection using fuzzy matching
    /// </summary>
    /// <typeparam name="T">Type of entity to match</typeparam>
    /// <param name="searchName">Name to search for</param>
    /// <param name="candidates">Collection of candidate entities</param>
    /// <param name="nameSelector">Function to extract name from entity</param>
    /// <returns>Best matching entity or null if no suitable match found</returns>
    public static T? FindBestMatch<T>(string searchName, IEnumerable<T> candidates, Func<T, string> nameSelector)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(searchName) || !candidates.Any())
            return null;

        var normalizedSearchName = NormalizeName(searchName);
        var bestMatch = default(T);
        var bestScore = 0.0;
        const double MINIMUM_MATCH_THRESHOLD = 0.7; // 70% similarity required

        foreach (var candidate in candidates)
        {
            var candidateName = nameSelector(candidate);
            if (string.IsNullOrWhiteSpace(candidateName))
                continue;

            var normalizedCandidateName = NormalizeName(candidateName);

            // Try exact match first (highest priority)
            if (normalizedSearchName == normalizedCandidateName)
            {
                return candidate;
            }

            // Try contains match (medium-high priority)
            if (normalizedCandidateName.Contains(normalizedSearchName) ||
                normalizedSearchName.Contains(normalizedCandidateName))
            {
                var containsScore = Math.Max(
                    (double)normalizedSearchName.Length / normalizedCandidateName.Length,
                    (double)normalizedCandidateName.Length / normalizedSearchName.Length
                ) * 0.9; // Slightly lower than exact match

                if (containsScore > bestScore)
                {
                    bestScore = containsScore;
                    bestMatch = candidate;
                }
            }

            // Calculate similarity score using Jaro-Winkler-like algorithm
            var similarityScore = CalculateSimilarity(normalizedSearchName, normalizedCandidateName);
            if (similarityScore > bestScore && similarityScore >= MINIMUM_MATCH_THRESHOLD)
            {
                bestScore = similarityScore;
                bestMatch = candidate;
            }
        }

        return bestScore >= MINIMUM_MATCH_THRESHOLD ? bestMatch : null;
    }

    /// <summary>
    /// Normalizes a name for consistent comparison
    /// Use Unicode normalization to properly handle diacritics while preserving base letters
    /// </summary>
    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Use Unicode normalization instead of dropping non-ASCII
        var normalized = RemoveDiacritics(name).ToUpperInvariant().Trim();

        // Remove common prefixes/suffixes and administrative markers
        normalized = RemoveCommonPrefixes(normalized);

        // Remove special characters and normalize spacing
        normalized = Regex.Replace(normalized, @"[^\w\s]", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }

    /// <summary>
    /// Properly removes diacritics while preserving base letters using Unicode normalization
    /// This correctly handles characters like 'ñ' in 'Las Piñas' by converting them to 'n' instead of dropping them
    /// </summary>
    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Normalize to FormD (canonical decomposition) - separates base characters from diacritics
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            // Include only base characters (not diacritics/combining marks)
            // This preserves letters like 'ñ' -> 'n', 'á' -> 'a', etc.
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark) // NonSpacingMark = diacritics/combining marks
            {
                stringBuilder.Append(c);
            }
        }

        // Normalize back to FormC (canonical composition) for consistency
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Removes common administrative prefixes and suffixes
    /// </summary>
    private static string RemoveCommonPrefixes(string name)
    {
        var prefixesToRemove = new[]
        {
            // Regional prefixes
            "REGION ", "REG ", "R ", 
            
            // Municipality/City prefixes
            "CITY OF ", "MUNICIPALITY OF ", "MUNICIPAL ", "CITY ", 
            
            // Province prefixes
            "PROVINCE OF ", "PROV OF ", "PROV ", 
            
            // Common administrative terms
            "ADMINISTRATIVE REGION", "AUTONOMOUS REGION",
            
            // Directional markers that might cause mismatches
            "NORTHERN ", "SOUTHERN ", "EASTERN ", "WESTERN ",
            "NORTH ", "SOUTH ", "EAST ", "WEST ",
            
            // Common geographic terms
            "ISLAND OF ", "ISLANDS OF ", "ISLAND ", "ISLANDS ",
        };

        var suffixesToRemove = new[]
        {
            // Regional suffixes  
            " REGION", " REG", 
            
            // Administrative suffixes
            " ADMINISTRATIVE REGION", " AUTONOMOUS REGION",
            " ADMINISTRATIVE", " AUTONOMOUS",
            
            // Province/City suffixes
            " PROVINCE", " PROV", " CITY", " MUNICIPALITY",
            
            // Geographic suffixes
            " ISLAND", " ISLANDS"
        };

        var result = name;

        // Remove prefixes
        foreach (var prefix in prefixesToRemove)
        {
            if (result.StartsWith(prefix))
            {
                result = result[prefix.Length..].Trim();
                break; // Only remove one prefix to avoid over-normalization
            }
        }

        // Remove suffixes
        foreach (var suffix in suffixesToRemove)
        {
            if (result.EndsWith(suffix))
            {
                result = result[..^suffix.Length].Trim();
                break; // Only remove one suffix to avoid over-normalization
            }
        }

        return result;
    }

    /// <summary>
    /// Calculates similarity between two strings using a combination of techniques
    /// </summary>
    private static double CalculateSimilarity(string s1, string s2)
    {
        if (s1 == s2) return 1.0;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0;

        // Use Levenshtein distance for basic similarity
        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);
        var similarity = 1.0 - ((double)distance / maxLength);

        // Apply bonus for word-level matches (handles reordering)
        var wordSimilarity = CalculateWordSimilarity(s1, s2);

        // Combine character-level and word-level similarities
        return Math.Max(similarity, wordSimilarity);
    }

    /// <summary>
    /// Calculates similarity based on word-level matching
    /// </summary>
    private static double CalculateWordSimilarity(string s1, string s2)
    {
        var words1 = s1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var words2 = s2.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words1.Length == 0 || words2.Length == 0)
            return 0.0;

        var matchedWords = 0;
        var usedWords2 = new HashSet<string>();

        foreach (var word1 in words1)
        {
            foreach (var word2 in words2)
            {
                if (usedWords2.Contains(word2))
                    continue;

                // Exact word match
                if (word1 == word2)
                {
                    matchedWords++;
                    usedWords2.Add(word2);
                    break;
                }

                // Partial word match (for abbreviations and variations)
                if (word1.Length >= 3 && word2.Length >= 3 &&
                    (word1.Contains(word2) || word2.Contains(word1)))
                {
                    matchedWords++;
                    usedWords2.Add(word2);
                    break;
                }
            }
        }

        // Return percentage of words matched
        return (double)matchedWords / Math.Max(words1.Length, words2.Length);
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings
    /// </summary>
    private static int LevenshteinDistance(string s1, string s2)
    {
        if (s1 == s2) return 0;
        if (s1.Length == 0) return s2.Length;
        if (s2.Length == 0) return s1.Length;

        var d = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            d[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            d[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[s1.Length, s2.Length];
    }
}