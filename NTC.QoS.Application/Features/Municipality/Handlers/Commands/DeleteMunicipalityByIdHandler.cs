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
        var municipalityQuery = _repository.Query(m => m.Id == request.Id);
        var municipality = await _repository.FirstOrDefaultAsync(municipalityQuery, cancellationToken);

        if (municipality is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Message = "Municipality not found.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullMunicipality", ["Municipality not found."]
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
