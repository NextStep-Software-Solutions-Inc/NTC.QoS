namespace NTC.QoS.Domain.Models.DTOs.AntennaPattern
{
    public class AntennaPatternDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string URL { get; set; } = string.Empty;
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
    }
}
