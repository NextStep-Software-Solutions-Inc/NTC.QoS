namespace NTC.QoS.Domain.Models.DTOs.Sector;

public class CreateSectorDto
{
    public required Guid NetworkId { get; set; }
    public required string AntennaType { get; set; }
    public required int Number { get; set; }
    public Guid? MIMOId { get; set; }
    public required Guid CellSectorId { get; set; }
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
    public required Guid TxAntennaPatternId { get; set; }
    public required decimal TxEIRP { get; set; }
    public string? RxAntennaModel { get; set; }
    public string? RxCableType { get; set; }
    public decimal RxCableLength { get; set; }
    public decimal RxCableLoss { get; set; }
    public decimal RxAdditionalLoss { get; set; }
    public decimal RxAntennaHeight { get; set; }
    public decimal RxAntennaGain { get; set; }
    public decimal RxAzimuth { get; set; }
    public decimal RxBeamTilt { get; set; }
    public Guid RxAntennaPatternId { get; set; }
    public decimal RxEIRP { get; set; }
}
