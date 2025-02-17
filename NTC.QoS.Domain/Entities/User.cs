namespace NTC.QoS.Domain.Entities;

public class User : BaseEntity
{
    public Guid UserTypeId { get; set; }
    public virtual UserType? UserType { get; set; }
    public string AuthUserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
}
