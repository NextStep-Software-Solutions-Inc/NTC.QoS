using System.Text.Json.Serialization;

namespace NTC.QoS.Domain.Models.DTOs.User;

public class InvitationDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }

    [JsonPropertyName("roleId")]
    public Guid? RoleId { get; set; }

    [JsonPropertyName("roleName")]
    public string? RoleName { get; set; }

    [JsonPropertyName("roleScope")]
    public string? RoleScope { get; set; }

    [JsonPropertyName("regions")]
    public List<InvitationRegionDto> Regions { get; set; } = new();
}

public class InvitationRegionDto
{
    [JsonPropertyName("regionId")]
    public Guid RegionId { get; set; }

    [JsonPropertyName("regionName")]
    public string RegionName { get; set; } = string.Empty;

    [JsonPropertyName("regionCode")]
    public string? RegionCode { get; set; }

    [JsonPropertyName("roleId")]
    public Guid? RoleId { get; set; }

    [JsonPropertyName("roleName")]
    public string? RoleName { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Active";
}
