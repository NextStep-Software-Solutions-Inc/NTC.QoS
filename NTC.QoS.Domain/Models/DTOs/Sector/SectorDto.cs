using NTC.QoS.Domain.Models.DTOs.CellSector;

namespace NTC.QoS.Domain.Models.DTOs.Sector;

public class SectorDto
{
    public required Guid Id { get; set; }
    public NetworkSectorDto? Network { get; set; }
    public required string AntennaType { get; set; }
    public MIMOSectorDto? MIMO { get; set; }
    public CellSectorDto? CellSector { get; set; }
    public string? RadioEquipment { get; set; }
    public bool? IsRxEqualTx { get; set; }
    public decimal? TxPower { get; set; }
    public string? TxAntennaModel { get; set; }
    public string? TxCableType { get; set; }
    public decimal? TxCableLength { get; set; }
    public decimal? TxCableLoss { get; set; }
    public decimal? TxAdditionalLoss { get; set; }
    public decimal? TxAntennaHeight { get; set; }
    public decimal? TxAntennaGain { get; set; }
    public decimal? TxAzimuth { get; set; }
    public decimal? TxBeamTilt { get; set; }
    public AntennaPatternSectorDto? TxAntennaPattern { get; set; }
    public decimal? TxEIRP { get; set; }
    public string? RxAntennaModel { get; set; }
    public string? RxCableType { get; set; }
    public decimal? RxCableLength { get; set; }
    public decimal? RxCableLoss { get; set; }
    public decimal? RxAdditionalLoss { get; set; }
    public decimal? RxAntennaHeight { get; set; }
    public decimal? RxAntennaGain { get; set; }
    public decimal? RxAzimuth { get; set; }
    public decimal? RxBeamTilt { get; set; }
    public AntennaPatternSectorDto? RxAntennaPattern { get; set; }
    public decimal? RxEIRP { get; set; }
}
