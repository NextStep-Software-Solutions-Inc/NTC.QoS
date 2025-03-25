namespace NTC.QoS.Domain.Models.DTOs.RolePermission;

public class GetRolePermissionDto
{
    public Guid Id { get; set; }
    public RolePermissionDto? Role { get; set; }
    public RolePermissionDto? Permission { get; set; }
}
