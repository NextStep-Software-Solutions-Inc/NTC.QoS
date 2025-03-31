using System.ComponentModel;

/// <summary>
/// Provides helper methods for working with enums of type T.
/// </summary>
/// <remarks>
/// This class offers functionalities for retrieving enum names, values, descriptions, and parsing user input into enum values.
/// It utilizes generic type constraints to ensure that T is a struct and an enum.
/// </remarks>
namespace NTC.QoS.Domain.Helper;

public static class EnumHelper<T> where T : struct, Enum
{
    /// <summary>
    /// Gets the name of the specified enum value.
    /// </summary>
    /// <param name="status">The enum value.</param>
    /// <returns>The name of the enum value, or "Unknown" if the value is default or cannot be found.</returns>
    public static string GetName(T status)
    {
        if (EqualityComparer<T>.Default.Equals(status, default))
        {
            return "Unknown";
        }
        return Enum.GetName(typeof(T), status) ?? "unknown";
    }

    /// <summary>
    /// Gets the enum value corresponding to the specified string.
    /// </summary>
    /// <param name="status">The string representation of the enum value.</param>
    /// <returns>The enum value.</returns>
    /// <exception cref="ArgumentException">Thrown when the string cannot be parsed into a valid enum value.</exception>
    public static T GetValue(string status)
    {
        if (Enum.TryParse(status, true, out T result))
        {
            return result;
        }
        else
        {
            throw new ArgumentException($"Invalid value for enum {typeof(T).Name}: {status}");
        }
    }

    /// <summary>
    /// Gets the description attribute of the specified enum value.
    /// </summary>
    /// <param name="value">The enum value.</param>
    /// <returns>The description of the enum value, or the enum value's name if no description is found.</returns>
    public static string GetDescription(T value)
    {
        var field = typeof(T).GetField(value.ToString());
        if (field is not null)
        {
            var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            return attribute?.Description ?? value.ToString();
        }
        else
        {
            return value.ToString();
        }
    }

    /// <summary>
    /// Parses a user input string into an enum value of type T.
    /// </summary>
    /// <param name = "userInput" > The user input string to parse.</param>
    /// <returns>The parsed enum value.</returns>
    /// <exception cref = "ArgumentException" > Thrown when the input string cannot be parsed into a valid enum value.</exception>
    /// <remarks>
    /// This method attempts to parse the input string in the following order:
    /// 1. Directly, using case-insensitive Enum.TryParse().
    /// 2. By comparing the input to the description attributes of the enum values, case-insensitively.
    /// 3. By comparing the input to the description attributes of the enum values after removing spaces, case-insensitively.
    /// 4. By removing spaces from the user input and then attempting a direct case-insensitive Enum.TryParse().
    /// If all parsing attempts fail, an ArgumentException is thrown.
    /// </remarks>
    public static T ParseUserInput(string userInput)
    {
        // 1. Attempt parsing directly (case-insensitive)
        if (Enum.TryParse(userInput, true, out T result))
        {
            return result;
        }

        // 2. Attempt parsing by description (case-insensitive)
        foreach (T enumValue in Enum.GetValues(typeof(T)))
        {
            if (GetDescription(enumValue).Equals(userInput, StringComparison.OrdinalIgnoreCase))
            {
                return enumValue;
            }
        }

        // 3. Attempt parsing by description after removing all spaces and trimming.
        foreach (T enumValue in Enum.GetValues(typeof(T)))
        {
            if (GetDescription(enumValue).Replace(" ", "").Trim().Equals(userInput.Replace(" ", "").Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return enumValue;
            }
        }

        // 4. Attempt parsing by removing all spaces and trimming from the input then compare to enum names.
        if (Enum.TryParse(userInput.Replace(" ", "").Trim(), true, out T _))
        {
            return result;
        }

        // If all parsing attempts fail, throw an exception
        throw new ArgumentException($"Invalid value for enum {typeof(T).Name}: '{userInput}'. Please use a valid name or description.");
    }
}