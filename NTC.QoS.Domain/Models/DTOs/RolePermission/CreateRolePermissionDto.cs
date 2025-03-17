namespace NTC.QoS.Domain.Models.DTOs.RolePermission;

public class CreateRolePermissionDto
{
    public required Guid RoleId { get; set; }
    public required Guid PermissionId { get; set; }
}