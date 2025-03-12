using System.ComponentModel;

namespace NTC.QoS.Domain.Enums;

public enum NetworkSystemTypeEnum
{
    [Description("Unknown")]
    Unknown = 0,

    [Description("Generic TRX")]
    GenericTRX = 1,

    [Description("LTE")]
    LTE = 2,

    [Description("5G")]
    _5G = 3,

    [Description("Terrestrial Broadcasting")]
    TerrestrialBroadcasting = 4,

    [Description("Air to Ground Radio")]
    AirToGroundRadio = 5,
}
