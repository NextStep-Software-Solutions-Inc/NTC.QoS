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
            .Select(n => new
            {
                n.Id,
                ProviderName = n.ServiceProvider.Provider != null ? n.ServiceProvider.Provider.Name : null,
                ProviderColor = n.ServiceProvider.Provider != null ? n.ServiceProvider.Provider.Color : null,
                Lat = n.Location.Coordinate.Y,
                Lng = n.Location.Coordinate.X
            });
        
        var napData = await _NAPRepository.FirstOrDefaultAsync(napDtoQuery, cancellationToken);
        
        if (napData is null)
        {
            return new ApiResponse<NAPDto>("NAP not found");
        }

        // Validate that Provider data exists (data integrity check)
        if (string.IsNullOrEmpty(napData.ProviderName))
        {
            return new ApiResponse<NAPDto>
            {
                Message = "Service provider data is missing or incomplete.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "DataIntegrity", ["The associated service provider information could not be retrieved."] }
                }
            };
        }

        return new ApiResponse<NAPDto>
        {
            Succeeded = true,
            Message = "NAP details retrieved successfully.",
            Data = new NAPDto
            {
                Id = napData.Id,
                ISP = napData.ProviderName,
                ISPColor = napData.ProviderColor ?? "",
                Lat = napData.Lat,
                Lng = napData.Lng,
                Dist = 0
            }
        };
    }
}
