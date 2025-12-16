using System.Security.Cryptography;
using NTC.QoS.Infrastructure.Seed.Interfaces;

namespace NTC.QoS.Infrastructure.Seed.Services;

public class PasswordGenerator : IPasswordGenerator
{
    public string GenerateSecurePassword()
    {
        // Character sets exclude visually ambiguous characters (I, L, O, 1, 0) to reduce transcription errors
        // when sharing credentials verbally or copying from logs. This improves readability and usability.
        const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // Excludes I, L, O
        const string lowercase = "abcdefghijkmnpqrstuvwxyz"; // Excludes l, o
        const string digits = "023456789"; // Excludes 1, 0
        const string special = "!@#$%^&*";
        const string allChars = uppercase + lowercase + digits + special;

        var password = new char[16];

        // Ensure at least one of each type
        password[0] = uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)];
        password[1] = lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        // Fill the rest randomly
        for (int i = 4; i < 16; i++)
        {
            password[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
        }

        // Shuffle the password using Fisher-Yates shuffle
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}