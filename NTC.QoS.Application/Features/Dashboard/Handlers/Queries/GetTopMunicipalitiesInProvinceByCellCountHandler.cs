using MediatR;
using NTC.QoS.Application.Features.Dashboard.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GeoEntity;

namespace NTC.QoS.Application.Features.Dashboard.Handlers.Queries;

public class GetTopMunicipalitiesInProvinceByCellCountHandler(IMunicipalityRepository municipalityRepository) : IRequestHandler<GetTopMunicipalitiesInProvinceByCellCountRequest, ApiResponse<List<GeoEntityCellCountDto>>>
{
    private readonly IMunicipalityRepository _municipalityRepository = municipalityRepository;

    public async Task<ApiResponse<List<GeoEntityCellCountDto>>> Handle(GetTopMunicipalitiesInProvinceByCellCountRequest request, CancellationToken cancellationToken)
    {
        var result = await _municipalityRepository.GetTopMunicipalitiesByCellCountAsync(
request.ProvinceId,
  request.Limit,
         request.StartDate,
            request.EndDate,
          cancellationToken);

   return new ApiResponse<List<GeoEntityCellCountDto>>
      {
    Succeeded = true,
         Message = $"Top municipalities in ProvinceId [{request.ProvinceId}] retrieved successfully.",
     Data = result
   };
    }
}
