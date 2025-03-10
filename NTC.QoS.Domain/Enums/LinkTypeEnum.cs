using System.ComponentModel;

namespace NTC.QoS.Domain.Enums;

public enum LinkTypeEnum
{
    [Description("Unknown")]
    Unknown = 0,

    [Description("Uplink")]
    Uplink = 1,

    [Description("Downlink")]
    Downlink = 2,
}
