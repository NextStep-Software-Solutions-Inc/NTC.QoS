using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class GetCellTechnologiesRequest : IRequest<ApiResponse<List<CellTechnologyBandDto>>>
{
    public Guid CellId { get; set; }
}
