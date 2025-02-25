using MediatR;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Province;

namespace NTC.QoS.Application.Features.Province.Handlers.Commands;

public class CreateProvinceRequestHandler(IRepository<Domain.Entities.Province> repository) : IRequestHandler<CreateProvinceRequest, ApiResponse<CreateProvinceDto>>
{
    private readonly IRepository<Domain.Entities.Province> _repository = repository;

    public async Task<ApiResponse<CreateProvinceDto>> Handle(CreateProvinceRequest request, CancellationToken cancellationToken)
    {
        var province = await _repository.FindAsync(p => p.Name == request.Province.Name && !p.IsDeleted, cancellationToken);

        if (province != null)
        {
            return new ApiResponse<CreateProvinceDto>
            {
                Message = "Province already exists.",
                Succeeded = false
            };
        }

        var newProvince = new Domain.Entities.Province
        {
            RegionId = request.Province.RegionId,
            Name = request.Province.Name,
            Description = request.Province.Description,
        };

        _repository.Add(newProvince);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreateProvinceDto>
        {
            Data = new CreateProvinceDto
            {
                RegionId = newProvince.RegionId,
                Name = newProvince.Name,
                Description = newProvince.Description,
            },

            Message = "Province created successfully.",
            Succeeded = true
        };
    }
}