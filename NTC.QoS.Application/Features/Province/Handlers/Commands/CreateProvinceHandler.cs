using MediatR;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Province.Handlers.Commands;

public class CreateProvinceRequestHandler(IProvinceRepository provinceRepository, IRegionRepository regionRepository)
    : IRequestHandler<CreateProvinceRequest, ApiResponse<ProvinceDto>>
{
    private readonly IProvinceRepository _provinceRepository = provinceRepository;
    private readonly IRegionRepository _regionRepository = regionRepository;

    public async Task<ApiResponse<ProvinceDto>> Handle(CreateProvinceRequest request, CancellationToken cancellationToken)
    {
        var existingProvinceQuery = _provinceRepository.Query(
            p => p.Name == request.Province.Name && !p.IsDeleted);
        var existingProvince = await _provinceRepository.FirstOrDefaultAsync(existingProvinceQuery, cancellationToken);

        if (existingProvince is not null)
        {
            return new ApiResponse<ProvinceDto>
            {
                Message = "Province already exists.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { 
                        "Name", ["A province with the specified name already exists."] 
                    }
                }
            };
        }

        var existsQuery = _provinceRepository.Query(p => p.Name == request.Province.Name && p.RegionId == request.Province.RegionId && !p.IsDeleted);
        var exists = await _provinceRepository.FirstOrDefaultAsync(existsQuery, cancellationToken);

        if (exists is not null)
        {
            return new ApiResponse<ProvinceDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Name", new[] { "A province with this name already exists in the selected region." }
                    }
                },
                Message = "Province name already exists in this region. Please choose a different name."
            };
        }

        var newProvince = new Domain.Entities.Province
        {
            RegionId = request.Province.RegionId,
            Name = request.Province.Name,
            Description = request.Province.Description
        };

        _provinceRepository.Add(newProvince);
        await _provinceRepository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ProvinceDto>
        {
            Data = new ProvinceDto
            {
                Id = newProvince.Id,
                Name = newProvince.Name,
                Description = newProvince.Description,
                Region = new RegionDto
                {
                    Id = newProvince.RegionId,
                    Name = newProvince.Region?.Name ?? string.Empty,
                    Description = newProvince.Region?.Description
                }
            },
            Message = "Province created successfully.",
            Succeeded = true
        };
    }
}
