using MediatR;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.Municipality.Handlers.Commands;

public class DeleteMunicipalityHandler(IMunicipalityRepository repository) : IRequestHandler<DeleteMunicipalityByIdRequest, ApiResponse<bool>>
{
    private readonly IMunicipalityRepository _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteMunicipalityByIdRequest request, CancellationToken cancellationToken)
    {
        var municipality = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (municipality is null)
        {
            return new ApiResponse<bool>
            {
                Data = false,
                Succeeded = false,
                Message = "The Municipality you are trying to delete does not exist.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Error", ["Municipality not found."]
                    }
                }
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
