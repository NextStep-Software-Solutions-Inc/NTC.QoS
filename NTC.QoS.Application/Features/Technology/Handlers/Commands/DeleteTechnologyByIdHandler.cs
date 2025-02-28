using MediatR;
using NTC.QoS.Application.Features.Technology.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Technology.Handlers.Commands;

public class DeleteTechnologyByIdHandler(IRepository<Domain.Entities.Technology> repository) : IRequestHandler<DeleteTechnologyByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Technology> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteTechnologyByIdRequest request,
            CancellationToken cancellationToken)
    {
        var technology = await _repository.FindAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken);

        if (technology == null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Error = new[] { "Technology not found." },
                Message = "The technology you are trying to delete does not exist."
            };
        }

        _repository.Delete(technology);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Error = new[] { "" },
            Message = $"Technology '{technology.Standard}' has been successfully deleted."
        };
    }
}