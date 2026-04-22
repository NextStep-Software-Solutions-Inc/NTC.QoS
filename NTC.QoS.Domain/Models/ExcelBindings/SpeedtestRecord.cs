using NTC.QoS.Domain.Attributes;

namespace NTC.QoS.Domain.Models.ExcelBindings;
public record SpeedtestRecord
{
    [ExcelHeader("timestamp")]
    public DateTime Timestamp { get; set; }

    [ExcelHeader("ping")]
    public string Ping { get; set; } = string.Empty;

    [ExcelHeader("jitter")]
    public string Jitter { get; set; } = string.Empty;

    [ExcelHeader("upload")]
    public decimal Upload { get; set; }

    [ExcelHeader("download")]
    public decimal Download { get; set; }

    [ExcelHeader("region")]
    public string Region { get; set; } = string.Empty;

    [ExcelHeader("province")]
    public string Province { get; set; } = string.Empty;

    [ExcelHeader("municipality")]
    public string Municipality { get; set; } = string.Empty;

    [ExcelHeader("barangay")]
    public string Barangay { get; set; } = string.Empty;

    [ExcelHeader("rssi")]
    public decimal? RSSI { get; set; }

    [ExcelHeader("signal_quality")]
    public string SignalQuality { get; set; } = string.Empty;

    [ExcelHeader("operator")]
    public string Provider { get; set; } = string.Empty;

    [ExcelHeader("lat")]
    public double Latitude { get; set; }

    [ExcelHeader("lon")]
    public double Longitude { get; set; }

    [ExcelHeader("phone_model")]
    public string PhoneModel { get; set; } = string.Empty;

    [ExcelHeader("email")]
    public string Email { get; set; } = string.Empty;

    [ExcelHeader("nro")]
    public string NRO { get; set; } = string.Empty;

    [ExcelHeader("network_type")]
    public string NetworkType { get; set; } = string.Empty;

    [ExcelHeader("cell_id")]
    public string CellIdentifier { get; set; } = string.Empty;

    [ExcelHeader("MCC")]
    public string MCC { get; set; } = string.Empty;

    [ExcelHeader("MNC")]
    public string MNC { get; set; } = string.Empty;

    [ExcelHeader("tac")]
    public string TAC { get; set; } = string.Empty;

    [ExcelHeader("success")]
    public string Success { get; set; } = string.Empty;

    [ExcelHeader("nbc")]
    public bool? IsNoBroadbandCoverage { get; set; }

}