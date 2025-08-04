using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Analysis.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analysis;

namespace NTC.QoS.Application.Features.Analysis.Handlers.Queries;

public class GetMunicipalityConnectivityAnalysisHandler : IRequestHandler<GetMunicipalityConnectivityAnalysisRequest, ApiResponse<MunicipalityConnectivityAnalysisDto>>
{
    private readonly IRepository<Domain.Entities.Speedtest> _speedtestRepository;
    private readonly IRepository<Domain.Entities.Municipality> _municipalityRepository;

    public GetMunicipalityConnectivityAnalysisHandler(
        IRepository<Domain.Entities.Speedtest> speedtestRepository,
        IRepository<Domain.Entities.Municipality> municipalityRepository)
    {
        _speedtestRepository = speedtestRepository;
        _municipalityRepository = municipalityRepository;
    }

    public async Task<ApiResponse<MunicipalityConnectivityAnalysisDto>> Handle(
        GetMunicipalityConnectivityAnalysisRequest request, 
        CancellationToken cancellationToken)
    {
        var municipalityQuery = _municipalityRepository.Query(m => m.Id == request.MunicipalityId);
        var municipality = await _municipalityRepository.FirstOrDefaultAsync(
            municipalityQuery.Select(m => new { m.Id, m.Name }),
            cancellationToken);

        if (municipality == null)
        {
            return new ApiResponse<MunicipalityConnectivityAnalysisDto>
            {
                Succeeded = false,
                Message = "Municipality not found.",
                Errors = new Dictionary<string, string[]>
                {
                    { "NullMunicipality", ["Municipality with the specified ID does not exist."] }
                }
            };
        }

        var cutoffDate = DateTime.UtcNow - request.TimeWindow;
        
        var speedtestQuery = _speedtestRepository.Query(s => 
            s.Address.Contains(municipality.Name) && 
            s.DateTime >= cutoffDate && 
            !s.IsDeleted);

        var speedtests = await _speedtestRepository.ToListAsync(
            speedtestQuery.Select(s => new
            {
                s.DownloadSpeed,
                s.UploadSpeed,
                s.Ping,
                s.Jitter,
                s.RSSI,
                s.SignalQuality,
                s.ServiceProviderId,
                s.CellIdentifier,
                s.DateTime
            }),
            cancellationToken);

        if (speedtests.Count == 0)
        {
            return new ApiResponse<MunicipalityConnectivityAnalysisDto>
            {
                Succeeded = true,
                Message = "No speedtest data available for analysis.",
                Data = new MunicipalityConnectivityAnalysisDto
                {
                    MunicipalityId = municipality.Id,
                    MunicipalityName = municipality.Name,
                    Classification = ServiceClassificationEnum.Unserved,
                    Score = 0,
                    Reason = "No speedtest data available",
                    DeficiencyAreas = new List<string> { "No speedtest data available for analysis" },
                    Metrics = new ConnectivityMetricsDto()
                }
            };
        }

        // Calculate metrics
        var speedCompliantTests = speedtests.Count(s => 
            s.DownloadSpeed >= request.MinDownloadSpeed && 
            s.UploadSpeed >= request.MinUploadSpeed);

        var metrics = new ConnectivityMetricsDto
        {
            AverageDownloadSpeed = speedtests.Average(s => s.DownloadSpeed),
            AverageUploadSpeed = speedtests.Average(s => s.UploadSpeed),
            AveragePing = speedtests.Average(s => s.Ping),
            AverageJitter = speedtests.Average(s => s.Jitter),
            AverageRSSI = speedtests.Average(s => s.RSSI),
            SpeedCompliantTests = speedCompliantTests,
            TotalTests = speedtests.Count,
            SpeedComplianceRate = speedtests.Count > 0 ? (decimal)speedCompliantTests / speedtests.Count * 100 : 0,
            UniqueProviders = speedtests.Select(s => s.ServiceProviderId).Distinct().Count(),
            UniqueCells = speedtests.Where(s => !string.IsNullOrEmpty(s.CellIdentifier))
                                   .Select(s => s.CellIdentifier).Distinct().Count(),
            EarliestTest = speedtests.Min(s => s.DateTime),
            LatestTest = speedtests.Max(s => s.DateTime),
            SignalQualityDistribution = speedtests
                .GroupBy(s => s.SignalQuality)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        // Calculate classification
        var deficiencies = new List<string>();
        var score = CalculateScore(metrics, request, deficiencies);
        var classification = score >= 75 ? ServiceClassificationEnum.Served :
                           score >= 40 ? ServiceClassificationEnum.Underserved :
                           ServiceClassificationEnum.Unserved;

        var result = new MunicipalityConnectivityAnalysisDto
        {
            MunicipalityId = municipality.Id,
            MunicipalityName = municipality.Name,
            Classification = classification,
            Score = score,
            Reason = GetClassificationReason(classification),
            DeficiencyAreas = deficiencies,
            Metrics = metrics
        };

        return new ApiResponse<MunicipalityConnectivityAnalysisDto>
        {
            Succeeded = true,
            Message = "Municipality connectivity analysis completed successfully.",
            Data = result
        };
    }

    private static decimal CalculateScore(ConnectivityMetricsDto metrics, GetMunicipalityConnectivityAnalysisRequest request, List<string> deficiencies)
    {
        decimal score = 0;

        // Data availability (10% weight)
        if (metrics.TotalTests >= request.MinTestSampleSize)
        {
            score += 10;
        }
        else
        {
            deficiencies.Add($"Insufficient test data ({metrics.TotalTests} tests, minimum {request.MinTestSampleSize} required)");
        }

        // Average speed criteria (40% weight)
        if (metrics.AverageDownloadSpeed >= request.MinDownloadSpeed && 
            metrics.AverageUploadSpeed >= request.MinUploadSpeed)
        {
            score += 40;
        }
        else
        {
            deficiencies.Add($"Average speeds below standards (Down: {metrics.AverageDownloadSpeed:F1} Mbps, Up: {metrics.AverageUploadSpeed:F1} Mbps)");
        }

        // Speed compliance rate (30% weight)
        if (metrics.SpeedComplianceRate >= 70)
        {
            score += 30;
        }
        else
        {
            deficiencies.Add($"Speed compliance rate too low ({metrics.SpeedComplianceRate:F1}%, minimum 70% required)");
        }

        // Latency criteria (20% weight)
        if (metrics.AveragePing <= request.MaxLatency)
        {
            score += 20;
        }
        else
        {
            deficiencies.Add($"High average latency ({metrics.AveragePing:F1} ms, maximum {request.MaxLatency} ms)");
        }

        return score;
    }

    private static string GetClassificationReason(ServiceClassificationEnum classification)
    {
        return classification switch
        {
            ServiceClassificationEnum.Served => "Meets broadband standards based on speedtest data",
            ServiceClassificationEnum.Underserved => "Partially meets broadband standards",
            ServiceClassificationEnum.Unserved => "Does not meet minimum broadband standards",
            _ => "Classification unknown"
        };
    }
}