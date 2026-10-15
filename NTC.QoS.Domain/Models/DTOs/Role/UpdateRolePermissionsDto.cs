namespace NTC.QoS.Domain.Models.DTOs.Role;

public class UpdateRolePermissionsDto
{
    public required List<string> Permissions { get; set; } = [];
}
