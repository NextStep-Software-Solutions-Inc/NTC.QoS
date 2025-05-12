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
        var NAPQuery = _NAPRepository.Query(n => n.Id.Equals(request.Id));

        var NAPDtoQuery = NAPQuery.Select(NAP => new NAPDto()
        {
            Id = NAP.Id,
            ServiceProvider = new()
            {
                Id = NAP.ServiceProvider.Id,
                Service = new()
                {
                    Id = NAP.ServiceProvider.Service.Id,
                    Name = NAP.ServiceProvider.Service.Name,
                    Type = EnumHelper<ServiceTypeEnum>.GetName(NAP.ServiceProvider.Service.Type)
                },
                Provider = new()
                {
                    Id = NAP.ServiceProvider.Provider.Id,
                    Name = NAP.ServiceProvider.Provider.Name,
                    Color = NAP.ServiceProvider.Provider.Color
                }
            },
            Address = NAP.Address,
            IsFileUploaded = NAP.IsFileUploaded,
            Latitude = NAP.Location.X,
            Longitude = NAP.Location.Y,
            Distance = Math.Round(NAP.Location.Distance(NAP.Location) * 1000, 2)
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
