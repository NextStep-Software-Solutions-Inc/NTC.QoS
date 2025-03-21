using MediatR;
using NTC.QoS.Application.Features.Province.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Province;

namespace NTC.QoS.Application.Features.Province.Handlers.Commands;

public class UpdateProvinceByIdHandler(IRepository<Domain.Entities.Province> repository) : IRequestHandler<UpdateProvinceRequest, ApiResponse<UpdateProvinceDto>>
{
    private readonly IRepository<Domain.Entities.Province> _repository = repository;
    public async Task<ApiResponse<UpdateProvinceDto>> Handle(UpdateProvinceRequest request,
            CancellationToken cancellationToken)
    {
        var province = await _repository.FindAsync(m => m.Id == request.Id && !m.IsDeleted, cancellationToken);


        if (string.IsNullOrEmpty(request.Province.Name))
        {
            return new ApiResponse<UpdateProvinceDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Name is required." },
                Message = request.Province.Name + " already exists in our system. Please try a different one."
            };
        }
        if (province == null)
        {
            return new ApiResponse<UpdateProvinceDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Province not found." },
                Message = "The province you are trying to edit does not exist."
            };
        }
        province.Name = request.Province.Name;
        province.Description = request.Province.Description;

         _repository.Update(province);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UpdateProvinceDto>
        {
            Succeeded = true,
            Data = new UpdateProvinceDto()
            {
                Name = request.Province.Name,
                Description = request.Province.Description,
            },
            Message = "Request successful! Your changes have been saved.."
        };
    }
}