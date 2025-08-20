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
        var serviceQuery = _repository.Query(s => s.Id == request.Id);
        var service = await _repository.FirstOrDefaultAsync(serviceQuery, cancellationToken);
        if (service is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullService", ["Service not found."]
                    }
                },
                Message = "The service you are trying to delete does not exist."
            };
        }

        _repository.Delete(service);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Message = "Service deleted successfully.",
            Succeeded = true
        };
    }
}
