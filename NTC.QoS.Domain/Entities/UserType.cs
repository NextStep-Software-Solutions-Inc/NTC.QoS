namespace NTC.QoS.Domain.Entities;


public class UserType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public virtual List<User>? Users { get; set; } = [];

}
