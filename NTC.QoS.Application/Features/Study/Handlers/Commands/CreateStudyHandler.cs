using MediatR;
using NTC.QoS.Application.Features.Study.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Handlers.Commands;

public class CreateStudyHandler(IRepository<Domain.Entities.Study> repository) : IRequestHandler<CreateStudyRequest, ApiResponse<StudyDto>>
{
    private readonly IRepository<Domain.Entities.Study> _repository = repository;
    public async Task<ApiResponse<StudyDto>> Handle(CreateStudyRequest request, CancellationToken cancellationToken)
    {
        var studyExist = await _repository.FindAsync(s => s.Name == request.Study.Name && !s.IsDeleted, cancellationToken);
        if (studyExist != null)
        {
            return new ApiResponse<StudyDto>
            {
                Message = "Study already exists",
                Succeeded = false
            };
        }
        var study = new Domain.Entities.Study
        {
            Name = request.Study.Name,
            Description = request.Study.Description,
            AreaStudyId = request.Study.AreaStudyId,
            StudyRadius = request.Study.StudyRadius
        };

        _repository.Add(study);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<StudyDto>
        {
            Data = new StudyDto
            {
                Id = study.Id,
                Name = study.Name,
                Description = study.Description,
                AreaStudyId = study.AreaStudyId,
                StudyRadius = study.StudyRadius
            },
            Message = "Study created successfully",
            Succeeded = true
        };
    }
}
