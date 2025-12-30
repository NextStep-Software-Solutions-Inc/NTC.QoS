using MediatR;

namespace NTC.QoS.Application.Features.BarangayCoverage.Requests;

public class DeleteBarangayCoverageByIdRequest : IRequest<Unit>
{
    public Guid Id { get; set; }
}
