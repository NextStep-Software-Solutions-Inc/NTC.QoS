using MediatR;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;
using NTC.QoS.Domain.Models.DTOs.Region;

namespace NTC.QoS.Application.Features.Province.Handlers.Commands;

public class UpdateProvinceByIdHandler(IProvinceRepository repository) : IRequestHandler<UpdateProvinceRequest, ApiResponse<ProvinceDto>>
{
    private readonly IProvinceRepository _repository = repository;
    public async Task<ApiResponse<ProvinceDto>> Handle(UpdateProvinceRequest request,
            CancellationToken cancellationToken)
    {
        var province = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (province is null)
        {
            return new ApiResponse<ProvinceDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Id", new[] { "Province not found." }
                    }
                },
                Message = "The province you are trying to edit does not exist."
            };
        }

        if (string.IsNullOrEmpty(request.Province.Name))
        {
            return new ApiResponse<ProvinceDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Name", new[] { "Province name is required." }
                    }
                },
                Message = "Province name is required."
            };
        }

        var exists = await _repository.FindAsync(
                p => p.Name == request.Province.Name && p.RegionId == province.RegionId,
                cancellationToken);

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

        province.Name = request.Province.Name;
        province.Description = request.Province.Description;

         _repository.Update(province);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<ProvinceDto>
        {
            Succeeded = true,
            Data = new ProvinceDto
            {
                Id = province.Id,
                Name = province.Name,
                Description = province.Description,
                Region = new RegionDto
                {
                    Id = province.RegionId,
                    Name = province.Region?.Name ?? string.Empty,
                    Description = province.Region?.Description
                }
            },
            Message = "Province updated successfully."
        };
    }
}