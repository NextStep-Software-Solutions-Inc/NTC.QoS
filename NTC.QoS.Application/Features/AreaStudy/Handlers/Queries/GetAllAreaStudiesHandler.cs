using MediatR;
using NTC.QoS.Application.Features.AreaStudy.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.AreaStudy;

namespace NTC.QoS.Application.Features.AreaStudy.Handlers.Queries;

public class GetAllAreaStudiesHandler(IRepository<Domain.Entities.AreaStudy> repository) : IRequestHandler<GetAllAreaStudiesRequest, PaginatedResponse<AreaStudyDto>>
{
    private readonly IRepository<Domain.Entities.AreaStudy> _repository = repository;   

    public async Task<PaginatedResponse<AreaStudyDto>> Handle(GetAllAreaStudiesRequest request, CancellationToken cancellationToken)
    {
        var allAreaStudies = await _repository.ListAsync(a => !a.IsDeleted, cancellationToken);

        var areaStudyDtos = allAreaStudies.Select(a => new AreaStudyDto
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description
        }).ToList();

        areaStudyDtos = [.. areaStudyDtos
            .Where(a => string.IsNullOrEmpty(request.Search) || (a.Name is not null && a.Name.Contains(request.Search, StringComparison.OrdinalIgnoreCase)))];

        if (areaStudyDtos.Count is 0)
        {
            return new PaginatedResponse<AreaStudyDto>(areaStudyDtos, request.PageNumber, request.PageSize)
            {
                Data = areaStudyDtos,
                Message = "No records found.",
                Succeeded = true
            };
        }

        return new PaginatedResponse<AreaStudyDto>(areaStudyDtos, request.PageNumber, request.PageSize)
        {
            Data = areaStudyDtos,
            Succeeded = true,
            Message = "Area studies retrieved successfully."
        };
    }
}
