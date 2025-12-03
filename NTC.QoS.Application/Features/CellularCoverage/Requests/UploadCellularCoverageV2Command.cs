using MediatR;
using Microsoft.AspNetCore.Http;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.CellularCoverage;

namespace NTC.QoS.Application.Features.CellularCoverage.Requests;

/// <summary>
/// Command to upload and process CellularCoverage V2 CSV file
/// User provides: NetworkId, ServiceProviderId, MunicipalityId
/// BandId and TechnologyId are derived from Network
/// </summary>
public class UploadCellularCoverageV2Command : IRequest<ApiResponse<CellularCoverageV2UploadResultDto>>
{
    public required Guid NetworkId { get; set; }
    public required Guid ServiceProviderId { get; set; }
    public required Guid MunicipalityId { get; set; }
    public required IFormFile CsvFile { get; set; }
}
