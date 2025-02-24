namespace NTC.QoS.Domain.Models.DTOs.Generation
{
    public class GenerationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }
}
