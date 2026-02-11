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
        // Use projection to fetch only required fields for optimal performance
        var napDtoQuery = _NAPRepository.Query(n => n.Id.Equals(request.Id))
            .Select(n => new NAPDto
            {
                Id = n.Id,
                ISP = n.ServiceProvider.Provider != null ? n.ServiceProvider.Provider.Name : "",
                ISPColor = n.ServiceProvider.Provider != null ? n.ServiceProvider.Provider.Color : "",
                Lat = n.Location.Coordinate.Y,
                Lng = n.Location.Coordinate.X,
                Dist = 0
            });
        
        var napDto = await _NAPRepository.FirstOrDefaultAsync(napDtoQuery, cancellationToken);
        
        if (napDto is null)
        {
            return new ApiResponse<NAPDto>("NAP not found");
        }

        return new ApiResponse<NAPDto>
        {
            Succeeded = true,
            Message = "NAP details retrieved successfully.",
            Data = napDto
        };
    }
}
