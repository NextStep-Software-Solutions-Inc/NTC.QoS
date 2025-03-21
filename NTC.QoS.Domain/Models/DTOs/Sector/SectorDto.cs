namespace NTC.QoS.Domain.Models.DTOs.Sector;

public class SectorDto
{
    public required Guid Id { get; set; }
    public required NetworkSectorDto Network { get; set; }
    public required string AntennaType { get; set; }
    public required int Number { get; set; }
    public required MIMOSectorDto MIMO { get; set; }
    public required CellSectorDto Cell { get; set; }
    public required string RadioEquipment { get; set; }
    public required bool IsRxEqualTx { get; set; }
    public required decimal TxPower { get; set; }
    public required string TxAntennaModel { get; set; }
    public required string TxCableType { get; set; }
    public required decimal TxCableLength { get; set; }
    public required decimal TxCableLoss { get; set; }
    public required decimal TxAdditionalLoss { get; set; }
    public required decimal TxAntennaHeight { get; set; }
    public required decimal TxAntennaGain { get; set; }
    public required decimal TxAzimuth { get; set; }
    public required decimal TxBeamTilt { get; set; }
    public required AntennaPatternSectorDto TxAntennaPattern { get; set; }
    public required decimal TxEIRP { get; set; }
    public required string RxAntennaModel { get; set; }
    public required string RxCableType { get; set; }
    public required decimal RxCableLength { get; set; }
    public required decimal RxCableLoss { get; set; }
    public required decimal RxAdditionalLoss { get; set; }
    public required decimal RxAntennaHeight { get; set; }
    public required decimal RxAntennaGain { get; set; }
    public required decimal RxAzimuth { get; set; }
    public required decimal RxBeamTilt { get; set; }
    public required AntennaPatternSectorDto RxAntennaPattern { get; set; }
    public required decimal RxEIRP { get; set; }
}
