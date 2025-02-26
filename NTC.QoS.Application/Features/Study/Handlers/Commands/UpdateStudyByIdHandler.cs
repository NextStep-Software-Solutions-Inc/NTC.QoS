using MediatR;
using NTC.QoS.Application.Features.Study.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Application.Features.Study.Handlers.Commands;

public class UpdateStudyByIdHandler(IRepository<Domain.Entities.Study> repository) : IRequestHandler<UpdateStudyRequest, ApiResponse<StudyDto>>
{
    private readonly IRepository<Domain.Entities.Study> _repository = repository;
    public async Task<ApiResponse<StudyDto>> Handle(UpdateStudyRequest request, CancellationToken cancellationToken)
    {
        var study = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (study == null)
        {
            return new ApiResponse<StudyDto>("Study not found");
        }
        study.Name = request.Study.Name;
        study.Description = request.Study.Description;
        study.AreaStudyId = request.Study.AreaStudyId;
        study.StudyRadius = request.Study.StudyRadius;

        _repository.Update(study);
        await _repository.SaveChangesAsync(cancellationToken);
        return new ApiResponse<StudyDto>(new StudyDto
        {
            Id = study.Id,
            Name = study.Name,
            Description = study.Description,
            AreaStudyId = study.AreaStudyId,
            StudyRadius = study.StudyRadius
        });
    }
}
