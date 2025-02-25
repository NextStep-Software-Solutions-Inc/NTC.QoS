using MediatR;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Application.Features.Municipality.Handlers.Commands;

public class UpdateMunicipalityByIdHandler(IRepository<Domain.Entities.Municipality> repository) : IRequestHandler<UpdateMunicipalityByIdRequest, ApiResponse<Domain.Entities.Municipality>>
{
    private readonly IRepository<Domain.Entities.Municipality> _repository = repository;
    public async Task<ApiResponse<Domain.Entities.Municipality>> Handle(UpdateMunicipalityByIdRequest request, CancellationToken cancellationToken)
    {
        var municipality = await _repository.FindAsync(m => m.Id == request.Id && !m.IsDeleted, cancellationToken);
        if (municipality == null)
        {
            return new ApiResponse<Domain.Entities.Municipality>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Municipality Not found." },
                Message = "The municipality you are trying to edit does not exist."
            };
        }
        municipality.Name = request.Municipality.Name;
        municipality.Description = request.Municipality.Description;
        municipality.IsCity = request.Municipality.IsCity;

        _repository.Update(municipality);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<Domain.Entities.Municipality>
        {
            Succeeded = true,
            Error = new[] { "" },
            Data = municipality,
            Message = "Request successful! Your changes have been saved."
        };
    }
}