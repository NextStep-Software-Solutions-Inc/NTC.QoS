using System.ComponentModel;

namespace NTC.QoS.Domain.Helper;

public static class EnumHelper<T> where T : struct, Enum
{
    public static string GetName(T status)
    {
        if (EqualityComparer<T>.Default.Equals(status, default))
        {
            return "Unknown";
        }
        return Enum.GetName(typeof(T), status) ?? "unknown";
    }

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

        // 3. Attempt parsing by description after removing spaces.
        foreach (T enumValue in Enum.GetValues(typeof(T)))
        {
            if (GetDescription(enumValue).Replace(" ", "").Equals(userInput.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
            {
                return enumValue;
            }
        }

        // 4. Attempt parsing by removing spaces from the input then compare to enum names.
        if (Enum.TryParse(userInput.Replace(" ", ""), true, out T _))
        {
            return result;
        }

        // If all parsing attempts fail, throw an exception
        throw new ArgumentException($"Invalid value for enum {typeof(T).Name}: '{userInput}'. Please use a valid name or description.");
    }
}
