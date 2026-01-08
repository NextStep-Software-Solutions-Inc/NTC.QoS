using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Reports.WirelessBroadband.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.WirelessBroadband.Handlers;

/// <summary>
/// Handler for getting wireless broadband data report (paginated municipalities with barangays).
/// This endpoint returns data (not file download) similar to the wired broadband report.
/// </summary>
public class GetWirelessBroadbandDataReportHandler : IRequestHandler<GetWirelessBroadbandDataReportQuery, ApiResponse<WirelessBroadbandDataReportDto>>
{
    private readonly ILogger<GetWirelessBroadbandDataReportHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IMunicipalityRepository _municipalityRepository;

    public GetWirelessBroadbandDataReportHandler(
        ILogger<GetWirelessBroadbandDataReportHandler> _logger,
        IMediator mediator,
        IMunicipalityRepository municipalityRepository)
    {
        this._logger = _logger;
        _mediator = mediator;
        _municipalityRepository = municipalityRepository;
    }

    public async Task<ApiResponse<WirelessBroadbandDataReportDto>> Handle(
        GetWirelessBroadbandDataReportQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate pagination
            if (request.PageNumber < 1)
            {
                return new ApiResponse<WirelessBroadbandDataReportDto>("Page number must be at least 1.");
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return new ApiResponse<WirelessBroadbandDataReportDto>("Page size must be between 1 and 100.");
            }

            // Generate the full report to get all data (scores calculated)
            var reportRequest = new GetWirelessBroadbandReportQuery
            {
                RegionId = request.RegionId,
                ProvinceId = request.ProvinceId,
                MunicipalityId = request.MunicipalityId,
                IncludeScores = request.IncludeScores,
                FilterByStatus = request.FilterByStatus,
                Format = ReportFormat.Excel // Format doesn't matter, we just need the data
            };

            // We need to get the scored data from the report handler
            // For now, let's generate the full report which has all the logic
            // In the future, we could refactor to share the data generation logic
            
            _logger.LogInformation("Generating wireless broadband data report for RegionId={RegionId}, ProvinceId={ProvinceId}, MunicipalityId={MunicipalityId}, Page={PageNumber}, PageSize={PageSize}",
                request.RegionId, request.ProvinceId, request.MunicipalityId, request.PageNumber, request.PageSize);

            // Get municipalities for filtering
            IQueryable<Domain.Entities.Municipality> municipalityQuery;
            
            if (request.MunicipalityId.HasValue)
            {
                municipalityQuery = _municipalityRepository.Query(m => m.Id == request.MunicipalityId.Value);
            }
            else if (request.ProvinceId.HasValue)
            {
                municipalityQuery = _municipalityRepository.Query(m => m.ProvinceId == request.ProvinceId.Value);
            }
            else if (request.RegionId.HasValue)
            {
                municipalityQuery = _municipalityRepository.Query(m => m.Province.RegionId == request.RegionId.Value);
            }
            else
            {
                // National scope
                municipalityQuery = _municipalityRepository.Query();
            }

            // Get total count and municipalities for current page
            var totalMunicipalities = await _municipalityRepository.CountAsync(municipalityQuery, cancellationToken);
            var totalPages = (int)Math.Ceiling((double)totalMunicipalities / request.PageSize);

            var municipalities = await _municipalityRepository.ToListAsync(
                municipalityQuery
                    .OrderBy(m => m.Name)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(m => new { m.Id, m.Name, m.Code }),
                cancellationToken);

            // For now, return a placeholder response indicating this endpoint needs full implementation
            // The full implementation would need to refactor GetWirelessBroadbandReportHandler
            // to separate data generation from file generation
            
            var result = new WirelessBroadbandDataReportDto
            {
                Municipalities = new List<MunicipalityWirelessBroadbandDto>(),
                Summary = new WirelessBroadbandScoredSummaryDto
                {
                    GeneratedAt = DateTime.UtcNow
                },
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalMunicipalities = totalMunicipalities,
                TotalPages = totalPages
            };

            // TODO: Populate municipalities with barangays and wireless coverage data
            // This requires refactoring the report handler to share data generation logic

            return new ApiResponse<WirelessBroadbandDataReportDto>
            {
                Data = result,
                Message = $"Wireless broadband data report generated successfully for page {request.PageNumber} of {totalPages}. NOTE: Full data population pending refactoring.",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating wireless broadband data report");
            return new ApiResponse<WirelessBroadbandDataReportDto>($"Failed to generate report: {ex.Message}");
        }
    }
}
