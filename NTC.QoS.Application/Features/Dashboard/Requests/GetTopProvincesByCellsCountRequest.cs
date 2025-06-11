using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Dashboard.Requests;

public class GetTopProvincesByCellsCountRequest : IRequest<ApiResponse<List<Dictionary<string, int>>>>
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Region { get; set; } = string.Empty;
}
