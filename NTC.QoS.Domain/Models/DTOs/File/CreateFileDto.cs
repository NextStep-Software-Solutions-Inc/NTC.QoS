namespace NTC.QoS.Domain.Models.DTOs.File;

public class CreateFileDto
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
