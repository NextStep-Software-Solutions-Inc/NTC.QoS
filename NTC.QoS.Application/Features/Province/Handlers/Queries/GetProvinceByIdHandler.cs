using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Province.Handlers.Queries;

public class GetProvinceByIdHandler(IProvinceRepository provinceRepository) : IRequestHandler<GetProvinceByIdRequest, ApiResponse<ProvinceDto>>
{
    private readonly IProvinceRepository _provinceRepository = provinceRepository;

    public async Task<ApiResponse<ProvinceDto>> Handle(GetProvinceByIdRequest request, CancellationToken cancellationToken)
    {
        var provinceQuery = _provinceRepository.Query(p => !p.IsDeleted && p.Id.Equals(request.Id));
        var province = await _provinceRepository.FirstOrDefaultAsync(provinceQuery, cancellationToken);

        if (province is null)
        {
            return new ApiResponse<ProvinceDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullProvince", ["Province not found."] 
                    }
                },
                Message = "Province with the specified ID does not exist."
            };
        }

        return new ApiResponse<ProvinceDto>
        {
            Succeeded = true,
            Message = "Province retrieved successfully.",
            Data = new ProvinceDto
            {
                Id = province.Id,
                Name = province.Name,
                Description = province.Description,
                Code = province.Code,
                Area = province.Area,
                Region = new RegionDto
                {
                    Id = province.RegionId,
                    Name = province.Region?.Name ?? string.Empty,
                    Description = province.Region?.Description
                }
            }
        };
    }
}
