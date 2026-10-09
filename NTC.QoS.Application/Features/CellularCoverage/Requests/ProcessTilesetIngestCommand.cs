using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.CellularCoverage.Requests;

public class ProcessTilesetIngestCommand : IRequest<ApiResponse<bool>>
{
    public Guid TilesetId { get; set; }
    public object? UploadResult { get; set; }
}
