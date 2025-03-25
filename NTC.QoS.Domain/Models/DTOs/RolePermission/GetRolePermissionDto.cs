namespace NTC.QoS.Domain.Models.DTOs.RolePermission;

public class GetRolePermissionDto
{
    public Guid Id { get; set; }
    public required RolePermissionDto Role { get; set; }
    public required RolePermissionDto Permission { get; set; }
}
