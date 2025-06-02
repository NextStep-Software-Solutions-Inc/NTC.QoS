using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.CellularCoverage.Requests;

public class DeleteCellularCoverageByNetworkIdRequest : IRequest<ApiResponse<bool>>
{
    public required Guid NetworkId { get; set; }
}
