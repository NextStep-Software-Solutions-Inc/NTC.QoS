using MediatR;
using NTC.QoS.Application.Features.StudyMunicipality.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.StudyMunicipality.Handlers.Commands;

public class DeleteStudyMunicipalityHandler(IRepository<Domain.Aggregates.StudyMunicipality> repository) : IRequestHandler<DeleteStudyMunicipalityByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Aggregates.StudyMunicipality> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteStudyMunicipalityByIdRequest request, CancellationToken cancellationToken)
    {
        var studyMunicipality = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (studyMunicipality is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullStudyMunicipality", ["Study Municipality not found."]
                    }
                },
                Message = "The study municipality you are trying to delete does not exist."
            };
        }

        _repository.Delete(studyMunicipality);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        {
            Succeeded = true,
            Data = true,
            Message = "Study Municipality deleted successfully."
        };
    }
}
