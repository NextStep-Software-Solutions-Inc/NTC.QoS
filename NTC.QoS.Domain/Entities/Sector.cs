using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Entities;

public class Sector : BaseEntity
{
    public Guid NetworkId { get; set; }
    public virtual Network? Network { get; set; }
    public Guid? MIMOId { get; set; }
    public virtual MIMO? MIMO { get; set; }
    public Guid CellId { get; set; }
    public virtual Cell? Cell { get; set; }
    public Guid TxAntennaPatternId { get; set; }
    public virtual AntennaPattern? TxAntennaPattern { get; set; }
    public Guid? RxAntennaPatternId { get; set; }
    public virtual AntennaPattern? RxAntennaPattern { get; set; }
    public AntennaTypeEnum AntennaType { get; set; } = AntennaTypeEnum.Unknown;
    public int Number { get; set; }
    public string RadioEquipment { get; set; } = string.Empty;
    public bool IsRxEqualTx { get; set; }
    public decimal TxPower { get; set; }
    public string TxAntennaModel { get; set; } = string.Empty;
    public string TxCableType { get; set; } = string.Empty;
    public decimal TxCableLength { get; set; }
    public decimal TxCableLoss { get; set; }
    public decimal TxAdditionalLoss { get; set; }
    public decimal TxAntennaHeight { get; set; }
    public decimal TxAntennaGain { get; set; }
    public decimal TxAzimuth { get; set; }
    public decimal TxBeamTilt { get; set; }
    public decimal TxEIRP { get; set; }
    public string RxAntennaModel { get; set; } = string.Empty;
    public string RxCableType { get; set; } = string.Empty;
    public decimal RxCableLength { get; set; }
    public decimal RxCableLoss { get; set; }
    public decimal RxAdditionalLoss { get; set; }
    public decimal RxAntennaHeight { get; set; }
    public decimal RxAntennaGain { get; set; }
    public decimal RxAzimuth { get; set; }
    public decimal RxBeamTilt { get; set; }
    public decimal RxEIRP { get; set; }
}
