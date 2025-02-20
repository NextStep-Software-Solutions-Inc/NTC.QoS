using MediatR;
using NTC.QoS.Application.Features.Service.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Service;

namespace NTC.QoS.Application.Features.Service.Handlers.Queries
{
    public class GetAllServicesHandler(IRepository<Domain.Entities.Service> repository) : IRequestHandler<GetAllServicesRequest, PaginatedResponse<ServiceDto>>
    {
        private readonly IRepository<Domain.Entities.Service> _repository = repository;
        public async Task<PaginatedResponse<ServiceDto>> Handle(GetAllServicesRequest request, CancellationToken cancellationToken)
        {
            var allServices = await _repository.ListAsync(s => s.IsDeleted != true, cancellationToken);

            var services = allServices.Select(service => new ServiceDto
            {
                Id = service.Id,
                Name = service.Name,
                Type = EnumHelper<ServiceTypeEnum>.GetName(service.Type),
                CreatedDate = service.CreatedAt,
                CreatedBy = service.CreatedBy,
                ModifiedDate = service.UpdatedAt,
                ModifiedBy = service.UpdatedBy,
                IsDeleted = service.IsDeleted
            }).ToList();
            return new PaginatedResponse<ServiceDto>(services, request.PageNumber, request.PageSize);
        }
    }
}
