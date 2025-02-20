using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Commands
{
    public class CreateProviderHandler(IRepository<Domain.Entities.Provider> repository) : IRequestHandler<CreateProviderRequest, ApiResponse<CreateProviderDto>>
    {
        private readonly IRepository<Domain.Entities.Provider> _repository = repository;

        public async Task<ApiResponse<CreateProviderDto>> Handle(CreateProviderRequest request, CancellationToken cancellationToken)
        {
            var provider =  await _repository.FindAsync(p => p.Name == request.Provider.Name && !p.IsDeleted, cancellationToken);

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

            _repository.Add(newProvider);
            await _repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<CreateProviderDto>
            {
                Data = new CreateProviderDto
                {
                    Name = newProvider.Name,
                    Description = newProvider.Description,
                    LicenseNumber = newProvider.LicenseNumber
                },
                Message = "Provider created successfully",
                Succeeded = true
            };
        }
    }
}