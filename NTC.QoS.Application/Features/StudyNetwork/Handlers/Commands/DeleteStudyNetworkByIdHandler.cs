using MediatR;
using NTC.QoS.Application.Features.StudyNetwork.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.StudyNetwork.Handlers.Commands;

public class DeleteStudyNetworkByIdHandler(IRepository<Domain.Aggregates.StudyNetwork> repository) : IRequestHandler<DeleteStudyNetworkByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Aggregates.StudyNetwork> _repository = repository;

    public async Task<ApiResponse<bool>> Handle(DeleteStudyNetworkByIdRequest request, CancellationToken cancellationToken)
    {
        var studyNetworkQuery = _repository.Query(s => s.Id == request.Id);
        var studyNetwork = await _repository.FirstOrDefaultAsync(studyNetworkQuery, cancellationToken);
        if (studyNetwork is null)
        {
            return new ApiResponse<bool>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullStudyNetwork", [ "Study Network not found."]
                    }
                },
                Message = "The study network you are trying to delete does not exists."
            };
        }

        _repository.Delete(studyNetwork);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<bool>
        { 
            Data = true,
            Succeeded = true,
            Message = "Study Network deleted successfully."
        };
    }
}
