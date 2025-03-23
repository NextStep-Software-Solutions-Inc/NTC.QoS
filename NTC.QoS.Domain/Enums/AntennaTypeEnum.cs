using System.ComponentModel;

namespace NTC.QoS.Domain.Enums;

public enum AntennaTypeEnum
{
    [Description("Unknown")]
    Unknown = 0,

    [Description("Omnidirectional")]
    Omnidirectional = 1,

    [Description("Sector")]
    Sector = 2,

    [Description("Panel")]
    Panel = 3,

    [Description("Yagi")]
    Yagi = 4,

    [Description("Parabolic Dish")]
    ParabolicDish = 5,

    [Description("Small Cell")]
    SmallCell = 6,
}
