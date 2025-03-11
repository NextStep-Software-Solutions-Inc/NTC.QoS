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
}
