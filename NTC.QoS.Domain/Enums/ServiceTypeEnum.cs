namespace NTC.QoS.Domain.Enums;

public enum ServiceTypeEnum
{
    Wired = 1,
    Wireless = 2,
    Unknown = 3,
    DSL = 4,
    Cellular = 5,
}

public static class ServiceTypeEnumExtensions
{
    public static bool IsMobile(this ServiceTypeEnum serviceType)
    {
        return serviceType == ServiceTypeEnum.Wireless
            || serviceType == ServiceTypeEnum.Cellular;
    }

    public static bool IsFixedLine(this ServiceTypeEnum serviceType)
    {
        return serviceType == ServiceTypeEnum.Wired
            || serviceType == ServiceTypeEnum.DSL;
    }
}
