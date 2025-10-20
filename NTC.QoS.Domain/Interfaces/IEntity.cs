namespace NTC.QoS.Domain.Interfaces;
public interface IEntity
{
    Guid Id { get; set; }
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime UpdatedAt { get; set; }
    string UpdatedBy { get; set; }
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; } 
    string? DeletedBy { get; set; } // made nullable to align with BaseEntity
}
