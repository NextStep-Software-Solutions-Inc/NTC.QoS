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
        var technologyQuery = _repository.Query(t => t.Id == request.Id && !t.IsDeleted);
        var technology = await _repository.FirstOrDefaultAsync(technologyQuery, cancellationToken);

        if (technology == null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {"Error", ["Technology not found."] }
                },
                Message = "The technology you are trying to delete does not exist."
            };
        }

        _repository.Delete(technology);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Data = true,
            Succeeded = true,
            Message = $"Technology '{technology.Standard}' has been successfully deleted."
        };
    }
}