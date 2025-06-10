using MediatR;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Handlers.Queries;

public class GetNAPByIdHandler(INAPRepository NAPRepository) : IRequestHandler<GetNAPByIdRequest, ApiResponse<NAPDto>>
{
    private readonly INAPRepository _NAPRepository = NAPRepository;

    public async Task<ApiResponse<NAPDto>> Handle(GetNAPByIdRequest request, CancellationToken cancellationToken)
    {
        var NAPQuery = _NAPRepository.Query(n => n.Id.Equals(request.Id) && !n.IsDeleted);

        var NAPDtoQuery = NAPQuery.Select(n => new NAPDto()
        {
            Id = n.Id,
            ISP = n.ServiceProvider.Provider.Name,
            ISPColor = n.ServiceProvider.Provider.Color,
            Lat = n.Location.Coordinate.Y,
            Lng = n.Location.Coordinate.X,
            Dist = 0
        });

        var NAP = await _NAPRepository.FirstOrDefaultAsync(NAPDtoQuery, cancellationToken);

        if(NAP is null)
        {
            return new ApiResponse<NAPDto>("NAP not found");
        }

        return new ApiResponse<NAPDto>
        {
            Succeeded = true,
            Message = "NAP details retrieved successfully.",
            Data = NAP

        };
    }
}
