using MediatR;
using NTC.QoS.Application.Features.Service.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Handlers.Commands
{
    public class CreateServiceHandler(IRepository<Domain.Entities.Service> repository) : IRequestHandler<CreateServiceRequest, ApiResponse<CreateServiceDto>>
    {
        private readonly IRepository<Domain.Entities.Service> _repository = repository;
        public async Task<ApiResponse<CreateServiceDto>> Handle(CreateServiceRequest request, CancellationToken cancellationToken)
        {

            var service = await _repository.FindAsync(s => s.Name == request.Service.Name && !s.IsDeleted, cancellationToken);
          if (service != null)
            {
                return new ApiResponse<CreateServiceDto>
                {
                    Message = "Service already exists",
                    Succeeded = false
                };
            }
            var newService = new Domain.Entities.Service
            {
                Name = request.Service.Name,
                Type = EnumHelper<ServiceTypeEnum>.GetValue( request.Service.Type),
            };
            _repository.Add(newService);
            await _repository.SaveChangesAsync(cancellationToken);
            return new ApiResponse<CreateServiceDto>
            {
                Data = new CreateServiceDto
                {
                    Name = newService.Name,
                    Type = EnumHelper<ServiceTypeEnum>.GetName( newService.Type)
                },
                Message = "Service created successfully",
                Succeeded = true
            };
        }
    }
}
