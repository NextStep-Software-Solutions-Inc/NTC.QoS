namespace NTC.QoS.Domain.Models.DTOs.File;

public class FileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;

}
