using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Reports;

namespace NTC.QoS.Application.Features.Reports.WiredBroadband.Requests;

public class GetWiredBroadbandReportQuery : IRequest<ApiResponse<WiredBroadbandReportDto>>
{
    public Guid? RegionId { get; set; }
    public Guid? ProvinceId { get; set; }
    public Guid? MunicipalityId { get; set; }
    public bool IncludeOnlyServedAreas { get; set; } = false;
    /// <summary>
    /// When true, forces generation of new connectivity assessments even if recent ones exist.
    /// When false (default), uses existing assessments from the last month if available.
    /// </summary>
    public bool ForceGenerateConnectivityAssessment { get; set; } = false;
}