namespace NTC.QoS.Domain.Models.DTOs.Cellular;

public class CreateCellularDto
{
    public Guid BarangayId { get; set; }
    public Guid CellId { get; set; }
    public Guid ServiceProviderId { get; set; }
    public Guid GenerationBandId { get; set; }
    public Guid FileId { get; set; }
    public int RowNumber { get; set; }
}
