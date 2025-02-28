using MediatR;
using NTC.QoS.Application.Features.Study.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Handlers.Queries;

public class GetStudyByIdHandler(IStudyRepository studyRepository) : IRequestHandler<GetStudyByIdRequest, ApiResponse<StudyDto>>
{
    private readonly IRepository<Domain.Entities.Study> _studyRepository = studyRepository;
    public async Task<ApiResponse<StudyDto>> Handle(GetStudyByIdRequest request, CancellationToken cancellationToken)
    {
        var studyExist = await _studyRepository.GetByIdAsync(request.Id, cancellationToken);
        if (studyExist == null)
        {
            return new ApiResponse<StudyDto>("Study not found");
        }
        var studyDto = new StudyDto
        {
            Id = studyExist.Id,
            Name = studyExist.Name,
            Description = studyExist.Description,
            AreaStudyId = studyExist.AreaStudyId,
            AreaStudy = studyExist != null ? new()
                {
                    Name = studyExist.AreaStudy!.Name,
                    Description = studyExist.AreaStudy.Description ?? ""
                }:null!,
            StudyRadius = studyExist!.StudyRadius
        };
        return new ApiResponse<StudyDto>(studyDto, true, "Study found.");
    }
}
