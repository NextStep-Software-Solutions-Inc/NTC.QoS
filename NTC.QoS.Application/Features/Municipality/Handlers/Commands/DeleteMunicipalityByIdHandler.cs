using MediatR;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.Municipality.Handlers.Commands;

public class DeleteMunicipalityHandler(IRepository<Domain.Entities.Municipality> repository) : IRequestHandler<DeleteMunicipalityByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Municipality> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteMunicipalityByIdRequest request, CancellationToken cancellationToken)
    {
        var municipality = await _repository.FindAsync(m => m.Id == request.Id && !m.IsDeleted, cancellationToken);
        if (municipality == null)
        {
            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Message = "Municipality not found."
            };
        }
        _repository.Delete(municipality);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<bool>
        {
            Succeeded = true,
            Data = true,
            Message = "Municipality deleted successfully."
        };
    }
}
