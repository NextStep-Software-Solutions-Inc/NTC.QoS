using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;

namespace NTC.QoS.Application.Features.CellularCoverage.Requests;

public class GetCellularCoverageByIdRequest(Guid id) : IRequest<ApiResponse<CellularCoverageDto>>
{
    public Guid Id { get; set; } = id;
}
