using System.ComponentModel.DataAnnotations;

namespace NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

public class CreateBarangayCoverageDto
{
    [Required(ErrorMessage = "BarangayId is required")]
    public Guid BarangayId { get; set; }
    
    [Range(0, 100, ErrorMessage = "WiredFtthPercentage must be between 0 and 100")]
    public decimal WiredFtthPercentage { get; set; }
    
    [Range(0, 100, ErrorMessage = "WirelessPercentage must be between 0 and 100")]
    public decimal WirelessPercentage { get; set; }
}
