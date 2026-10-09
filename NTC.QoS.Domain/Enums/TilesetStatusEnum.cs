namespace NTC.QoS.Domain.Enums;

/// <summary>
/// Status of a MapTiler tileset ingestion lifecycle
/// </summary>
public enum TilesetStatusEnum
{
    Pending = 1,
    Uploading = 2,
    Processing = 3,
    Ready = 4,
    Failed = 5
}
