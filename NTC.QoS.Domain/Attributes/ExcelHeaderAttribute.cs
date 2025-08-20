namespace NTC.QoS.Domain.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ExcelHeaderAttribute : Attribute
{
    public string HeaderName { get; }

    public ExcelHeaderAttribute(string headerName)
    {
        HeaderName = headerName;
    }
}