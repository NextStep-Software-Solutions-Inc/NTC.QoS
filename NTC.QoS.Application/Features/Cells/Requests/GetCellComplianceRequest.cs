using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;

namespace NTC.QoS.Application.Features.Cells.Requests;

public class GetCellComplianceRequest : IRequest<ApiResponse<CellComplianceDto>>
{
    public Guid CellId { get; set; }
}
