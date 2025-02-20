using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Commands
{
    public class UpdateProviderByIdRequestHandler(IRepository<Domain.Entities.Provider> repository) : IRequestHandler<UpdateProviderByIdRequest, ApiResponse<Domain.Entities.Provider>>
    {
        private readonly IRepository<Domain.Entities.Provider> _repository = repository;
        public async Task<ApiResponse<Domain.Entities.Provider>> Handle(UpdateProviderByIdRequest request,
                CancellationToken cancellationToken)
        {
            var provider = await _repository.FindAsync(m => m.Id == request.Id && !m.IsDeleted, cancellationToken);


            if (string.IsNullOrEmpty(request.Provider.Name))
            {
                return new ApiResponse<Domain.Entities.Provider>
                {
                    Data = null,
                    Succeeded = false,
                    Error = new[] { "Name is required." },
                    Message = request.Provider.Name + " already exists in our system. Please try a different one."
                };
            }
            if (provider == null)
            {
                return new ApiResponse<Domain.Entities.Provider>
                {
                    Data = null,
                    Succeeded = false,
                    Error = new[] { "Provider not found." },
                    Message = "The provider you are trying to edit does not exist."
                };
            }
            provider.Name = request.Provider.Name;
            provider.Description = request.Provider.Description;
            provider.LicenseNumber = request.Provider.LicenseNumber;

             _repository.Update(provider);
            await _repository.SaveChangesAsync(cancellationToken);
            return new ApiResponse<Domain.Entities.Provider>
            {
                Succeeded = true,
                Error = new[] {""},
                Data = provider,
                Message = "Request successful! Your changes have been saved.."
            };
        }
    }
}