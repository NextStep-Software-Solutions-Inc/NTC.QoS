using MediatR;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Handlers.Queries;

public class GetNAPByIdHandler(INAPRepository NAPRepository) : IRequestHandler<GetNAPByIdRequest, ApiResponse<NAPDto>>
{
    private readonly INAPRepository _NAPRepository = NAPRepository;

    public async Task<ApiResponse<NAPDto>> Handle(GetNAPByIdRequest request, CancellationToken cancellationToken)
    {
        var napQuery = _NAPRepository.Query(n => n.Id.Equals(request.Id));
        var nap = await _NAPRepository.FirstOrDefaultAsync(napQuery, cancellationToken);
        if (nap is null)
        {
            return new ApiResponse<NAPDto>("NAP not found");
        }

        return new ApiResponse<NAPDto>
        {
            Succeeded = true,
            Message = "NAP details retrieved successfully.",
            Data = new NAPDto
            {
                Id = nap.Id,
                ISP = nap.ServiceProvider.Provider.Name,
                ISPColor = nap.ServiceProvider.Provider.Color,
                Lat = nap.Location.Coordinate.Y,
                Lng = nap.Location.Coordinate.X,
            }
        };
    }
}
