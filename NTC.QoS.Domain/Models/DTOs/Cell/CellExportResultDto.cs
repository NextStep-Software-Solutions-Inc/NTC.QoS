namespace NTC.QoS.Domain.Models.DTOs.Cell;

public class CellExportResultDto
{
    public byte[] FileContent { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}

public class CellExportItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string ServiceProviderName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string TechnologyBands { get; set; } = string.Empty;
}
