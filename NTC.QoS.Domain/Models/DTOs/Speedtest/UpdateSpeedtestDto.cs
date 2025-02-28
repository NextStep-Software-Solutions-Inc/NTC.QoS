using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.Barangay;
using NTC.QoS.Domain.Models.DTOs.Cell;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs.Generation;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Domain.Models.DTOs.Speedtest
{
    public class UpdateSpeedtestDto
    {
        public decimal Ping { get; set; }
        public decimal Jitter { get; set; }
        public decimal RSSI { get; set; }
        public decimal UploadSpeed { get; set; }
        public decimal DownloadSpeed { get; set; }
        public string CoorLatitude { get; set; } = string.Empty;
        public string CoorLongitude { get; set; } = string.Empty;
        public string PhoneModel { get; set; } = string.Empty;
        public string MCC { get; set; } = string.Empty;
        public string MNC { get; set; } = string.Empty;
        public string TAC { get; set; } = string.Empty;
        public SignalQualityEnum SignalQuality { get; set; } = SignalQualityEnum.Unknown;
    }
}
