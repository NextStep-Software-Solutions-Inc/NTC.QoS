using MediatR;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Commands
{
    public class CreateProviderRequestHandler : IRequestHandler<CreateProviderRequest, ApiResponse<CreateProviderDto>>
    {
        private readonly IRepository<Domain.Entities.Provider> _repository;
        public CreateProviderRequestHandler(IRepository<Domain.Entities.Provider> repository)
        {
            _repository = repository;
        }
        public async Task<ApiResponse<CreateProviderDto>> Handle(CreateProviderRequest request, CancellationToken cancellationToken)
        {
            var provider = await _repository.Find(p => p.Name == request.Provider.Name && p.IsDeleted == false)
     .FirstOrDefaultAsync(cancellationToken: cancellationToken);
            if (provider != null)
            {
                return new ApiResponse<CreateProviderDto>
                {
                    Message = "Provider already exists",
                    Succeeded = false
                };
            }
            var newProvider = new Domain.Entities.Provider
            {
                Name = request.Provider.Name,
                Description = request.Provider.Description,
                LicenseNumber = request.Provider.LicenseNumber
            };

            await _repository.AddAsync(newProvider);
            await _repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<CreateProviderDto>
            {
                Data = request.Provider,
                Message = "Provider created successfully",
                Succeeded = true
            };
        }
    }

}