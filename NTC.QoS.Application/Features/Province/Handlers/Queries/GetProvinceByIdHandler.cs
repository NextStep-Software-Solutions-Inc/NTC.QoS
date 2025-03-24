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
        var province = await _provinceRepository.GetByIdAsync(request.Id, cancellationToken);

        if (province == null)
        {
            return new ApiResponse<ProvinceDto>
            {
                Succeeded = false,
                Message = "Province not found."
            };
        }

        return new ApiResponse<ProvinceDto>
        {
            Succeeded = true,
            Message = "Province retrieved successfully",
            Data = new ProvinceDto()
            {
                Id = province.Id,
                Name = province.Name,
                Description = province.Description,
                Region = new RegionDto
                {
                    Id = province.RegionId,
                    Name = province.Region?.Name ?? "Unknown",
                    Description = province.Region?.Description
                }
            }
        };
    }
}
