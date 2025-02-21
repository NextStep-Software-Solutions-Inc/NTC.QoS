using MediatR;
using NTC.QoS.Application.Features.Service.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Handlers.Commands;

public class UpdateServiceHandler(IRepository<Domain.Entities.Service> repository) : IRequestHandler<UpdateServiceByIdRequest, ApiResponse<UpdateServiceDto>>
{
    private readonly IRepository<Domain.Entities.Service> _repository = repository;
    public async Task<ApiResponse<UpdateServiceDto>> Handle(UpdateServiceByIdRequest request, CancellationToken cancellationToken)
    {
        var service = await _repository.FindAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);
        if (service == null)
        {
            return new ApiResponse<UpdateServiceDto>
            {
                Message = "Service not found",
                Succeeded = false
            };
        }
        service.Name = request.Service.Name;
        service.Type = EnumHelper<ServiceTypeEnum>.GetValue(request.Service.Type);

        _repository.Update(service);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<UpdateServiceDto>
        {
            Data = new UpdateServiceDto
            {
                Name = service.Name,
                Type = EnumHelper<ServiceTypeEnum>.GetName(service.Type)
            },
            Message = "Service updated successfully",
            Succeeded = true
        };
    }
}
