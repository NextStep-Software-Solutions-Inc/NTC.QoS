namespace NTC.QoS.Domain.Models.DTOs.Sector;

public class UpdateSectorDto
{
    public Guid? NetworkId { get; set; }
    public string? AntennaType { get; set; }
    public int? Number { get; set; }
    public Guid? MIMOId { get; set; }
    public Guid? CellId { get; set; }
    public string? RadioEquipment { get; set; }
    public  bool? IsRxEqualTx { get; set; }
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
    public Guid? TxAntennaPatternId { get; set; }
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
    public Guid? RxAntennaPatternId { get; set; }
    public decimal? RxEIRP { get; set; }
}
