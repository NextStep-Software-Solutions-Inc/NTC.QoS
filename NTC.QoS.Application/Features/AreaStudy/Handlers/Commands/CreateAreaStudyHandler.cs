using MediatR;
using NTC.QoS.Application.Features.AreaStudy.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.Application.Features.AreaStudy.Handlers.Commands;

public class CreateAreaStudyHandler(IRepository<Domain.Entities.AreaStudy> repository) : IRequestHandler<CreateAreaStudyRequest, ApiResponse<AreaStudyDto>>
{
    private readonly IRepository<Domain.Entities.AreaStudy> _repository = repository;
    public async Task<ApiResponse<AreaStudyDto>> Handle(CreateAreaStudyRequest request, CancellationToken cancellationToken)
    {
        var areaStudy = await _repository.FindAsync(a => a.Name == request.AreaStudy.Name, cancellationToken);
        if (areaStudy is not null && areaStudy.IsDeleted.Equals(true)) 
        {
            areaStudy.IsDeleted = false;
            _repository.Update(areaStudy);
            await _repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<AreaStudyDto>
            {
                Data = new AreaStudyDto
                {
                    Id = areaStudy.Id,
                    Name = areaStudy.Name,
                    Description = request.AreaStudy.Description
                },
                Succeeded = true,
                Message = "Area Study restored successfully."
            };
        }

        if (areaStudy is not null && areaStudy.IsDeleted.Equals(false))
        {
            return new ApiResponse<AreaStudyDto>
            {
                Message = "Area Study already exists.",
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "Duplicate", ["Area study with the same name already exists."]
                    }
                },
                Succeeded = false
            };
        }

        var newAreaStudy = new Domain.Entities.AreaStudy
        {
            Name = request.AreaStudy.Name,
            Description = request.AreaStudy.Description
        };

        _repository.Add(newAreaStudy);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<AreaStudyDto>
        {
            Data = new AreaStudyDto
            {
                Id = newAreaStudy.Id,
                Name = newAreaStudy.Name,
                Description = newAreaStudy.Description
            },
            Message = "Area Study created successfully.",
            Succeeded = true
        };
    }
}
