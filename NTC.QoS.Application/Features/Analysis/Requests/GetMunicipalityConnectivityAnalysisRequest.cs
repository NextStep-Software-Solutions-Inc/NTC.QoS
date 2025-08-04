using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Analysis;

namespace NTC.QoS.Application.Features.Analysis.Requests;

public record GetMunicipalityConnectivityAnalysisRequest(
    Guid MunicipalityId,
    TimeSpan TimeWindow,
    decimal MinDownloadSpeed,
    decimal MinUploadSpeed,
    decimal MaxLatency,
    int MinTestSampleSize
) : IRequest<ApiResponse<MunicipalityConnectivityAnalysisDto>>;