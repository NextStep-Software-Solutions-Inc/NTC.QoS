using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;

namespace NTC.QoS.Application.Features.CellularCoverage.Requests;

public class CreateCellularCoverageRequest : IRequest<ApiResponse<CellularCoverageDto>>
{
    public required CreateCellularCoverageDto CellularCoverage { get; set; }
}
