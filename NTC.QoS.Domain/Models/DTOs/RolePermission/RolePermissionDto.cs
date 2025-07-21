using NTC.QoS.Domain.Models.DTOs.Permission;
using NTC.QoS.Domain.Models.DTOs.Role;

namespace NTC.QoS.Domain.Models.DTOs.RolePermission;

public class RolePermissionDto
{
    public Guid Id { get; set; }
    public PermissionDto? Permission { get; set; }
    public RoleDto? Role { get; set; }
}
