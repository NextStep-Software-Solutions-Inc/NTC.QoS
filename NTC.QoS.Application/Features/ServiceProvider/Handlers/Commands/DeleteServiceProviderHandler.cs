using MediatR;
using NTC.QoS.Application.Features.ServiceProvider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.ServiceProvider.Handlers.Commands;

public class DeleteServiceProviderHandler(IRepository<Domain.Aggregates.ServiceProvider> repository) : IRequestHandler<DeleteServiceProviderByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Aggregates.ServiceProvider> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteServiceProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var serviceProvider = await _repository.FindAsync(sp => sp.Id == request.Id && !sp.IsDeleted, cancellationToken);
        if (serviceProvider == null)
        {
            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Message = "Service Provider not found"
            };
        }
        serviceProvider.IsDeleted = true;
        _repository.Update(serviceProvider);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<bool>
        {
            Succeeded = true,
            Data = true,
            Message = "Service Provider deleted successfully"
        };
    }
}
