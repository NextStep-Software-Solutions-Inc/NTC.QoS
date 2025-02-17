namespace NTC.QoS.Domain.Entities;
public class Study : BaseEntity
{
    public Guid AreaStudyId { get; set; }
    public virtual AreaStudy? AreaStudy { get; set; }   
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal StudyRadius { get; set; }
}
