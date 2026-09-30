namespace NTC.QoS.Domain.Models.DTOs.User;

public class InviteUserDto
{
    public required string Email { get; set; } = string.Empty;
    public required Guid RoleId { get; set; }
    public string? RedirectUrl { get; set; }
    public List<InviteUserRegionDto> Regions { get; set; } = [];
}

public class InviteUserRegionDto
{
    public Guid RegionId { get; set; }
    public Guid? RoleId { get; set; }
    public string Status { get; set; } = "Active";
}