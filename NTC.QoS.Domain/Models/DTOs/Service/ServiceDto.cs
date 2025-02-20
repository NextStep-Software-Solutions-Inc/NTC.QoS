namespace NTC.QoS.Domain.Models.DTOs.Service;
public class ServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime? CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedDate { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
