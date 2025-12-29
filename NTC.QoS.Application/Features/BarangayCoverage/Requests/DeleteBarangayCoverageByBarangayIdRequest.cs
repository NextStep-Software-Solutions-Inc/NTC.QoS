using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.BarangayCoverage.Requests;

public class DeleteBarangayCoverageByBarangayIdRequest : IRequest<ApiResponse<bool>>
{
    public DeleteBarangayCoverageByBarangayIdRequest()
    {
    }

    public DeleteBarangayCoverageByBarangayIdRequest(Guid barangayId)
    {
        BarangayId = barangayId;
    }

    public Guid BarangayId { get; set; }
}
