using MediatR;
using NTC.QoS.Application.Features.Service.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Service.Handlers.Commands;

public class DeleteServiceHandler(IRepository<Domain.Entities.Service> repository) : IRequestHandler<DeleteServiceByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Service> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteServiceByIdRequest request, CancellationToken cancellationToken)
    {
        var service = await _repository.FindAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);
        if (service == null)
        {
            return new ApiResponse<bool>
            {
                Message = "Service not found",
                Succeeded = false
            };
        }
        service.IsDeleted = true;
        _repository.Update(service);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<bool>
        {
            Data = true,
            Message = "Service deleted successfully",
            Succeeded = true
        };
    }
}
