namespace NTC.QoS.Domain.Enums;

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
}
