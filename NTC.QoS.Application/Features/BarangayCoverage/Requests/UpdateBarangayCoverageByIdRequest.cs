using MediatR;
using NTC.QoS.Domain.Models.DTOs.BarangayCoverage;

namespace NTC.QoS.Application.Features.BarangayCoverage.Requests;

public class UpdateBarangayCoverageByIdRequest : IRequest<Unit>
{
    public Guid Id { get; set; }
    public required UpdateBarangayCoverageDto Coverage { get; set; }
}
