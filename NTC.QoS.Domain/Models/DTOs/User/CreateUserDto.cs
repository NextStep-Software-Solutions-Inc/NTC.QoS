namespace NTC.QoS.Domain.Models.DTOs.User;

public class CreateUserDto
{
    public required Guid RoleId { get; set; }
    public required string AuthUserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public List<CreateUserRegionAssignmentDto>? Regions { get; set; }
}

public class CreateUserRegionAssignmentDto
{
    public Guid RegionId { get; set; }
    public Guid? RoleId { get; set; }
    public string Status { get; set; } = "Active";
}
