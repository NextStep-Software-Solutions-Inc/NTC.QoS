namespace NTC.QoS.Domain.Models.DTOs.Cellular;

public class GetCellularDto
{
    public Guid Id { get; set; }
    public CommonCellularDto? Barangay { get; set; }
    public CommonCellularDto? Cell { get; set; }
    public ServiceProviderCellularDto? ServiceProvider { get; set; }
    public GenerationBandCellularDto? GenerationBand { get; set; }
    public FileCellularDto? File { get; set; }
    public int RowNumber { get; set; }
}
